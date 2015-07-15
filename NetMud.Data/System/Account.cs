﻿using NetMud.Data.Game;
using NetMud.DataAccess;
using NetMud.DataStructure.Base.System;
using NetMud.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMud.Data.System
{
    public class Account : IAccount
    {
        public string GlobalIdentityHandle { get; set; }

        public Account()
        {
            GlobalIdentityHandle = "Nobody";
        }

        public Account(string handle)
        {
            GlobalIdentityHandle = handle;
        }

        public long CurrentlySelectedCharacter { get; set; }

        private IList<ICharacter> _characters;
        public IEnumerable<ICharacter> Characters 
        { 
            get
            {
                if(_characters == null)
                {
                    var dataWrapper = new DataWrapper();
                    IEnumerable<ICharacter> returnValue = dataWrapper.GetAllBySharedKey<Character>("AccountHandle", GlobalIdentityHandle);
                    _characters = returnValue.ToList();
                }

                //Cause there are none
                if (_characters == null)
                    _characters = new List<ICharacter>();

                return _characters;
            }
            set
            {
                _characters = value.ToList();
            }
        }

        public string AddCharacter(ICharacter newChar)
        {
            var dataWrapper = new DataWrapper();
            IEnumerable<ICharacter> systemChars = dataWrapper.GetAll<Character>();

            if (systemChars.Any(ch => ch.GivenName.Equals(newChar.GivenName) && newChar.SurName.Equals(newChar.SurName)))
                return "A character with that name already exists, please choose another.";

            newChar.AccountHandle = GlobalIdentityHandle;
            newChar.Create();

            _characters.Add(newChar);

            return String.Empty;
        }

        public static IAccount GetByHandle(string handle)
        {
            var sql = String.Format("select * from [dbo].[Account] where GlobalIdentityHandle = '{0}'", handle);

            var ds = SqlWrapper.RunDataset(sql, CommandType.Text);
            IAccount account = null;

            if (ds.HasErrors)
            {
                //TODO: Error handling logging?
            }
            else if (ds.Rows != null)
            {
                foreach (DataRow dr in ds.Rows)
                {
                    try
                    {
                        account = Fill(dr);
                    }
                    catch
                    {
                        //error logging
                    }
                }
            }

            return account;
        }

        private static IAccount Fill(DataRow dr)
        {
            string outHandle = default(string);
            DataUtility.GetFromDataRow<string>(dr, "GlobalIdentityHandle", ref outHandle);

            return new Account(outHandle);
        }
    }
}
