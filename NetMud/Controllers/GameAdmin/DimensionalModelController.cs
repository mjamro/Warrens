﻿using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using NetMud.Authentication;
using NetMud.Data.Reference;
using NetMud.DataAccess;
using NetMud.DataAccess.Cache;
using NetMud.Models.Admin;
using System;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace NetMud.Controllers.GameAdmin
{
    public class DimensionalModelController : Controller
    {
        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public DimensionalModelController()
        {
        }

        public DimensionalModelController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ActionResult Index(string SearchTerms = "", int CurrentPageNumber = 1, int ItemsPerPage = 20)
        {
            var vModel = new ManageDimensionalModelDataViewModel(BackingDataCache.GetAll<DimensionalModelData>());
            vModel.authedUser = UserManager.FindById(User.Identity.GetUserId());

            vModel.CurrentPageNumber = CurrentPageNumber;
            vModel.ItemsPerPage = ItemsPerPage;
            vModel.SearchTerms = SearchTerms;

            return View("~/Views/GameAdmin/DimensionalModel/Index.cshtml", vModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(long ID, string authorize)
        {
            string message = string.Empty;

            if (string.IsNullOrWhiteSpace(authorize) || !ID.ToString().Equals(authorize))
                message = "You must check the proper authorize radio button first.";
            else
            {
                var authedUser = UserManager.FindById(User.Identity.GetUserId());

                var obj = BackingDataCache.Get<DimensionalModelData>(ID);

                if (obj == null)
                    message = "That does not exist";
                else if (obj.Remove())
                {
                    LoggingUtility.LogAdminCommandUsage("*WEB* - RemoveDimensionalModelData[" + ID.ToString() + "]", authedUser.GameAccount.GlobalIdentityHandle);
                    message = "Delete Successful.";
                }
                else
                    message = "Error; Removal failed.";
            }

            return RedirectToAction("Index", new { Message = message });
        }

        [HttpGet]
        public ActionResult Add()
        {
            var vModel = new AddEditDimensionalModelDataViewModel();
            vModel.authedUser = UserManager.FindById(User.Identity.GetUserId());

            return View("~/Views/GameAdmin/DimensionalModel/Add.cshtml", vModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(AddEditDimensionalModelDataViewModel vModel, HttpPostedFileBase modelFile)
        {
            string message = string.Empty;
            var authedUser = UserManager.FindById(User.Identity.GetUserId());

            try
            {
                if (modelFile.ContentLength == 0)
                    message = "You must post a comma delimited file with the model in it.";

                byte[] bytes = new byte[modelFile.InputStream.Length];
                modelFile.InputStream.Read(bytes, 0, (int)modelFile.InputStream.Length);
                var fileContents = Encoding.UTF8.GetString(bytes);

                var newObj = new DimensionalModelData(fileContents);
                newObj.Name = vModel.NewName;
                newObj.ModelType = vModel.NewModelType;

                if (newObj.IsModelValid())
                {
                    if (newObj.Create() == null)
                        message = "Error; Creation failed.";
                    else
                    {
                        LoggingUtility.LogAdminCommandUsage("*WEB* - AddDimensionalModelData[" + newObj.ID.ToString() + "]", authedUser.GameAccount.GlobalIdentityHandle);
                        message = "Creation Successful.";
                    }
                }
                else
                    message = "Invalid model file; Model files must contain 11 planes of a tag name followed by 11 rows of 11 nodes.";
            }
            catch (Exception ex)
            {
                LoggingUtility.LogError(ex);
                message = "Error; Creation failed.";
            }

            return RedirectToAction("Index", new { Message = message });
        }
    }
}