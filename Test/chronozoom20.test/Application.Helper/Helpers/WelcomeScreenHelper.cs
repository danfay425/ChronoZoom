﻿using System;
using Application.Driver;
using Application.Helper.UserActions;
using OpenQA.Selenium;

namespace Application.Helper.Helpers
{
    public class WelcomeScreenHelper : DependentActions
    {
        public void CloseWelcomePopup()
        {
            Logger.Log("<-");
            ClickCloseButton();
            WaitCondition(() => Convert.ToBoolean(GetJavaScriptExecutionResult("visReg != undefined")), 60);
            Logger.Log("->");
        }

        public void StartExploring()
        {
            Logger.Log("<-");
            Click(By.XPath("//*[text()='Start Exploring!']"));
            Logger.Log("->");
        }

        public bool IsWelcomeScreenDispalyed()
        {
            Logger.Log("<-");
            bool result = IsElementDisplayed(By.Id("welcomeScreen"));
            Logger.Log("-> result: " + result);
            return result;
        }
    }
}