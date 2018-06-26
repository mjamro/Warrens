﻿using NetMud.Authentication;
using NetMud.DataStructure.Base.Place;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace NetMud.Models.Admin
{
    public class ManageLocaleDataViewModel : PagedDataModel<ILocaleData>, BaseViewModel
    {
        public ApplicationUser authedUser { get; set; }

        public ManageLocaleDataViewModel(IEnumerable<ILocaleData> items)
            : base(items)
        {
            CurrentPageNumber = 1;
            ItemsPerPage = 20;
        }

        internal override Func<ILocaleData, bool> SearchFilter
        {
            get
            {
                return item => item.Name.ToLower().Contains(SearchTerms.ToLower());
            }
        }
    }

    public class AddEditLocaleDataViewModel : BaseViewModel
    {
        public ApplicationUser authedUser { get; set; }
        public long ZoneId { get; set; }

        public AddEditLocaleDataViewModel()
        {
        }

        [StringLength(100, ErrorMessage = "The {0} must be between {2} and {1} characters long.", MinimumLength = 2)]
        [Display(Name = "Name", Description = "The descriptive name for this Locale. Shows above the output window of the client.")]
        [DataType(DataType.Text)]
        public string Name { get; set; }

        public ILocaleData DataObject { get; set; }
    }
}