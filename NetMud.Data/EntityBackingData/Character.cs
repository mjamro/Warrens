﻿using NetMud.DataAccess;
using NetMud.DataStructure.Base.EntityBackingData;
using NetMud.DataStructure.Base.System;
using NetMud.DataStructure.SupportingClasses;
using NetMud.Utility;
using System;
using System.Data;
using System.Text;

namespace NetMud.Data.EntityBackingData
{
    public class Character : ICharacter
    {
        public Type EntityClass
        {
            get { return typeof(Game.Player); }
        }

        public long ID { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastRevised { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }

        public string SurName { get; set; }
        public string AccountHandle { get; set; }
        public StaffRank GamePermissionsRank { get; set; }

        public string LastKnownLocation { get; set; }
        public string LastKnownLocationType { get; set; }

        private IAccount _account;
        public IAccount Account
        {
            get
            {
                if (_account == null && !string.IsNullOrWhiteSpace(AccountHandle))
                    _account = System.Account.GetByHandle(AccountHandle);

                return _account;
            }
        }

        public string FullName()
        {
            return string.Format("{0} {1}", Name, SurName);
        }

        public void Fill(global::System.Data.DataRow dr)
        {
            long outId = default(long);
            DataUtility.GetFromDataRow<long>(dr, "ID", ref outId);
            ID = outId;

            string outAccountHandle = default(string);
            DataUtility.GetFromDataRow<string>(dr, "AccountHandle", ref outAccountHandle);
            AccountHandle = outAccountHandle;

            DateTime outCreated = default(DateTime);
            DataUtility.GetFromDataRow<DateTime>(dr, "Created", ref outCreated);
            Created = outCreated;

            DateTime outRevised = default(DateTime);
            DataUtility.GetFromDataRow<DateTime>(dr, "LastRevised", ref outRevised);
            LastRevised = outRevised;

            string outSurName = default(string);
            DataUtility.GetFromDataRow<string>(dr, "SurName", ref outSurName);
            SurName = outSurName;

            string outGivenName = default(string);
            DataUtility.GetFromDataRow<string>(dr, "Name", ref outGivenName);
            Name = outGivenName;

            string outGender = default(string);
            DataUtility.GetFromDataRow<string>(dr, "Gender", ref outGender);
            Gender = outGender;

            StaffRank outRank = StaffRank.Player;
            DataUtility.GetFromDataRow<StaffRank>(dr, "GamePermissionsRank", ref outRank);
            GamePermissionsRank = outRank;

            string outLKL = default(string);
            DataUtility.GetFromDataRow<string>(dr, "LastKnownLocation", ref outLKL);
            LastKnownLocation = outLKL;

            string outLKLT = default(string);
            DataUtility.GetFromDataRow<string>(dr, "LastKnownLocationType", ref outLKLT);
            LastKnownLocationType = outLKLT;
        }


        public int CompareTo(IData other)
        {
            if (other != null)
            {
                try
                {
                    if (other.GetType() != typeof(Character))
                        return -1;

                    if (other.ID.Equals(this.ID))
                        return 1;

                    return 0;
                }
                catch (Exception ex)
                {
                    LoggingUtility.LogError(ex);
                }
            }

            return -99;
        }

        public bool Equals(IData other)
        {
            if (other != default(IData))
            {
                try
                {
                    return other.GetType() == typeof(Character) && other.ID.Equals(this.ID);
                }
                catch (Exception ex)
                {
                    LoggingUtility.LogError(ex);
                }
            }

            return false;
        }

        public IData Create()
        {
            ICharacter returnValue = default(ICharacter);
            var sql = new StringBuilder();
            sql.Append("insert into [dbo].[Character]([SurName], [Name], [AccountHandle], [Gender])");
            sql.AppendFormat(" values('{0}','{1}','{2}', '{3}', {4})", SurName, Name, AccountHandle, Gender, GamePermissionsRank);
            sql.Append(" select * from [dbo].[Character] where ID = Scope_Identity()");

            try
            {
                var ds = SqlWrapper.RunDataset(sql.ToString(), CommandType.Text);

                if (ds.Rows != null)
                {
                    foreach (DataRow dr in ds.Rows)
                    {
                        Fill(dr);
                        returnValue = this;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingUtility.LogError(ex);
            }

            return returnValue;
        }

        public bool Remove()
        {
            var sql = new StringBuilder();
            sql.AppendFormat("remove from [dbo].[Character] where ID = {0}", ID);

            SqlWrapper.RunNonQuery(sql.ToString(), CommandType.Text);

            return true;
        }

        public bool Save()
        {
            var sql = new StringBuilder();
            sql.Append("update [dbo].[Character] set ");
            sql.AppendFormat(" [SurName] = '{0}' ", SurName);
            sql.AppendFormat(" , [Name] = '{0}' ", Name);
            sql.AppendFormat(" , [AccountHandle] = '{0}' ", AccountHandle);
            sql.AppendFormat(" , [Gender] = '{0}' ", Gender);
            sql.AppendFormat(" , [GamePermissionsRank] = {0} ", GamePermissionsRank);
            sql.AppendFormat(" , [LastKnownLocation] = '{0}' ", LastKnownLocation);
            sql.AppendFormat(" , [LastKnownLocationType] = '{0}' ", LastKnownLocationType);
            sql.AppendFormat(" , [LastRevised] = GetUTCDate()");
            sql.AppendFormat(" where ID = {0}", ID);

            SqlWrapper.RunNonQuery(sql.ToString(), CommandType.Text);

            return true;
        }
    }
}
