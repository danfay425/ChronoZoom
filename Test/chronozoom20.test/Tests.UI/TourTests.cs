﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TourTests : TestBase
    {
        public TestContext TestContext { get; set; }

        #region Initialize and Cleanup

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
           
        }

        [TestInitialize]
        public void TestInitialize()
        {
            BrowserStateManager.RefreshState();
            NavigationHelper.OpenHomePage();
            WelcomeScreenHelper.CloseWelcomePopup();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CreateScreenshotsIfTestFail(TestContext);
            NavigationHelper.NavigateToCosmos();
        }

        #endregion 

        [TestMethod]
        [Ignore]
        public void Test_Start_Pause_Tour()
        {
            TourHelper.OpenToursListWindow();
            TourHelper.SelectMayanHistoryTour();
            TourHelper.PauseTour();
            TourHelper.ResumeTour();
        }  
        
        [TestMethod]
        [Ignore]
        public void Test_Show_Hide_Bookmark_Tour()
        {
            TourHelper.OpenToursListWindow();
            TourHelper.SelectMayanHistoryTour();
            TourHelper.PauseTour();
            Assert.IsTrue(BookmarkHelper.IsBookmarkExpanded());
            BookmarkHelper.HideBookmark();
            Assert.IsFalse(BookmarkHelper.IsBookmarkExpanded());
        }
    }
}