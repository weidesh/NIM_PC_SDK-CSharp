﻿/** @file NIMFriendAPI.cs
  * @brief NIM SDK提供的friend接口
  * @copyright (c) 2015, NetEase Inc. All rights reserved
  * @author Harrison
  * @date 2015/12/8
  */

using System;
using NimUtility;
using NIM.Friend.Delegate;

namespace NIM.Friend
{
    /// <summary>
    /// 提供好友相关功能
    /// </summary>
    public class FriendAPI
    {
        private static FriendInfoChangedDelegate _friendInfoChangedHandler;
        private static GetFriendProfileDelegate _getFriendProfileCompleted;
        public static EventHandler<NIMFriendProfileChangedArgs> FriendProfileChangedHandler;

        private static readonly GetFriendsListDelegate OnGetFriendListCompleted = (resCode, retJson, je, ptr) =>
        {
            if (ptr != IntPtr.Zero)
            {
                var friends = string.IsNullOrEmpty(retJson) ? null : NIMFriends.Deserialize(retJson);
                ptr.InvokeOnce<GetFriendsListResultDelegate>(friends);
            }
        };

        internal static void RegisterCallbacks()
        {
            _friendInfoChangedHandler = OnFriendInfoChanged;
            _getFriendProfileCompleted = OnGetFriendProfileCompleted;
            RegFriendInfoChangedCb();
        }

        /// <summary>
        ///     获取缓存好友列表
        /// </summary>
        /// <param name="cb"></param>
        public static void GetFriendsList(GetFriendsListResultDelegate cb)
        {
            var ptr = DelegateConverter.ConvertToIntPtr(cb);
            FriendNativeMethods.nim_friend_get_list("", OnGetFriendListCompleted, ptr);
        }

        /// <summary>
        ///     统一注册好友变更通知回调函数（多端同步添加、删除、更新，好友列表同步）
        /// </summary>
        private static void RegFriendInfoChangedCb()
        {
            FriendNativeMethods.nim_friend_reg_changed_cb("", _friendInfoChangedHandler, IntPtr.Zero);
        }

        /// <summary>
        ///     添加、验证好友
        /// </summary>
        /// <param name="accid">对方账号</param>
        /// <param name="verifyType">验证类型</param>
        /// <param name="msg"></param>
        /// <param name="cb">操作结果回调</param>
        public static void ProcessFriendRequest(string accid, NIMVerifyType verifyType, string msg, FriendOperationDelegate cb)
        {
            FriendNativeMethods.nim_friend_request(accid, verifyType, msg, null, cb, IntPtr.Zero);
        }

        /// <summary>
        ///     获取缓存好友信息
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="cb"></param>
        public static void GetFriendProfile(string accountId, GetFriendProfileResultDelegate cb)
        {
            var ptr = DelegateConverter.ConvertToIntPtr(cb);
            FriendNativeMethods.nim_friend_get_profile(accountId, null, _getFriendProfileCompleted, ptr);
        }

        private static void OnGetFriendProfileCompleted(string accid, string profileJson, string jsonExtension, IntPtr userData)
        {
            if (userData != IntPtr.Zero)
            {
                NIMFriendProfile profile = null;
                if (!string.IsNullOrEmpty(profileJson))
                    profile = NIMFriendProfile.Deserialize(profileJson);
                userData.Invoke<GetFriendProfileResultDelegate>(accid, profile);
            }
        }


        /// <summary>
        ///     删除好友
        /// </summary>
        /// <param name="accid">对方账号</param>
        /// <param name="cb">操作结果回调</param>
        public static void DeleteFriend(string accid, FriendOperationDelegate cb)
        {
            FriendNativeMethods.nim_friend_delete(accid, null, cb, IntPtr.Zero);
        }

        /// <summary>
        ///     更新好友资料
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="cb"></param>
        public static void UpdateFriendInfo(NIMFriendProfile profile, FriendOperationDelegate cb)
        {
            if (profile == null || string.IsNullOrEmpty(profile.AccountId))
                throw new ArgumentException("profile or accountid can't be null");
            var jsonParam = profile.Serialize();
            FriendNativeMethods.nim_friend_update(jsonParam, null, cb, IntPtr.Zero);
        }

        private static void OnFriendInfoChanged(NIMFriendChangeType type, string resultJson, string jsonExtension, IntPtr userData)
        {
            if (FriendProfileChangedHandler != null)
            {
                INIMFriendChangedInfo IChangedInfo = null;
                if (!string.IsNullOrEmpty(resultJson))
                {
                    switch (type)
                    {
                        case NIMFriendChangeType.kNIMFriendChangeTypeDel:
                            IChangedInfo = FriendDeletedInfo.Deserialize(resultJson);
                            break;
                        case NIMFriendChangeType.kNIMFriendChangeTypeRequest:
                            IChangedInfo = FriendRequestInfo.Deserialize(resultJson);
                            break;
                        case NIMFriendChangeType.kNIMFriendChangeTypeSyncList:
                            IChangedInfo = FriendListSyncInfo.Deserialize(resultJson);
                            break;
                        case NIMFriendChangeType.kNIMFriendChangeTypeUpdate:
                            IChangedInfo = FriendUpdatedInfo.Deserialize(resultJson);
                            break;
                    }
                }
                var args = new NIMFriendProfileChangedArgs(IChangedInfo);
                FriendProfileChangedHandler(null, args);
            }
        }
    }
}