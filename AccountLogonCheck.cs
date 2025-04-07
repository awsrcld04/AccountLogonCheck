//Description:
//Finds accounts that have not logged in within a certain number of days
//Disables accounts that have not logged in within a certain number of days
//Optional: Find and disable accounts that have not been used (no login datetimestamp)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using Microsoft.Win32;
using System.Management;
using System.Reflection;
using System.Diagnostics;
using ActiveDs;

namespace AccountLogonCheck
{
    class ALCMain
    {
        struct AccountLoginParams
        {
            //public string strDisabledUsersLocation;
            public string strDisableUnusedAccounts;
            public string strUseExclusionGroups;
            public int intDaysSinceLastLogon;
            public int intDaysSinceDateCreated;
            public List<string> lstExcludePrefix;
            public List<string> lstExclude;
            public List<string> lstExcludeSuffix;
        }

        struct CMDArguments
        {
            public bool bParseCmdArguments;
        }

        static bool funcLicenseCheck()
        {
            try
            {
                string strLicenseString = "";
                bool bValidLicense = false;

                TextReader tr = new StreamReader("sotfwlic.dat");

                try
                {
                    strLicenseString = tr.ReadLine();

                    if (strLicenseString.Length > 0 & strLicenseString.Length < 29)
                    {
                        // [DebugLine] Console.WriteLine("if: " + strLicenseString);
                        Console.WriteLine("Invalid license");

                        tr.Close(); // close license file

                        return bValidLicense;
                    }
                    else
                    {
                        tr.Close(); // close license file
                        // [DebugLine] Console.WriteLine("else: " + strLicenseString);

                        string strMonthTemp = ""; // to convert the month into the proper number
                        string strDate;

                        //Month
                        strMonthTemp = strLicenseString.Substring(7, 1);
                        if (strMonthTemp == "A")
                        {
                            strMonthTemp = "10";
                        }
                        if (strMonthTemp == "B")
                        {
                            strMonthTemp = "11";
                        }
                        if (strMonthTemp == "C")
                        {
                            strMonthTemp = "12";
                        }
                        strDate = strMonthTemp;

                        //Day
                        strDate = strDate + "/" + strLicenseString.Substring(16, 1);
                        strDate = strDate + strLicenseString.Substring(6, 1);

                        // Year
                        strDate = strDate + "/" + strLicenseString.Substring(24, 1);
                        strDate = strDate + strLicenseString.Substring(4, 1);
                        strDate = strDate + strLicenseString.Substring(1, 2);

                        // [DebugLine] Console.WriteLine(strDate);
                        // [DebugLine] Console.WriteLine(DateTime.Today.ToString());
                        DateTime dtLicenseDate = DateTime.Parse(strDate);
                        // [DebugLine]Console.WriteLine(dtLicenseDate.ToString());

                        if (dtLicenseDate >= DateTime.Today)
                        {
                            bValidLicense = true;
                        }
                        else
                        {
                            Console.WriteLine("License expired.");
                        }

                        return bValidLicense;
                    }

                } //end of try block on tr.ReadLine

                catch
                {
                    // [DebugLine] Console.WriteLine("catch on tr.Readline");
                    Console.WriteLine("Invalid license");
                    tr.Close();
                    return bValidLicense;

                } //end of catch block on tr.ReadLine

            } // end of try block on new StreamReader("sotfwlic.dat")

            catch (System.Exception ex)
            {
                // [DebugLine] System.Console.WriteLine("{0} exception caught here.", ex.GetType().ToString());

                // [DebugLine] System.Console.WriteLine(ex.Message);

                if (ex.Message.StartsWith("Could not find file"))
                {
                    Console.WriteLine("License file not found.");
                }
                else
                {
                    MethodBase mb1 = MethodBase.GetCurrentMethod();
                    funcGetFuncCatchCode(mb1.Name, ex);
                }

                return false;

            } // end of catch block on new StreamReader("sotfwlic.dat")
        }

        static bool funcLicenseActivation()
        {
            try
            {
                if (funcCheckForFile("TurboActivate.dll"))
                {
                    if (funcCheckForFile("TurboActivate.dat"))
                    {
                        TurboActivate.VersionGUID = "4935355894e0da3d4465e86.37472852";

                        if (TurboActivate.IsActivated())
                        {
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("A license for this product has not been activated.");
                            return false;
                        }
                    }
                    else
                    {
                        Console.WriteLine("TurboActivate.dat is required and could not be found.");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("TurboActivate.dll is required and could not be found.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return false;
            }
        }

        static void funcPrintParameterWarning()
        {
            Console.WriteLine("A parameter is missing or is incorrect.");
            Console.WriteLine("Run AccountLogonCheck -? to get the parameter syntax.");
        }

        static void funcPrintParameterSyntax()
        {
            Console.WriteLine("AccountLogonCheck v1.0 (c) 2011 SystemsAdminPro.com");
            Console.WriteLine();
            Console.WriteLine("Description: Find and disable user accounts that are no longer being used");
            Console.WriteLine();
            Console.WriteLine("Parameter syntax:");
            Console.WriteLine();
            Console.WriteLine("Use the following required parameter:");
            Console.WriteLine("-run                     required parameter");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("AccountLogonCheck -run");
        }

        static CMDArguments funcParseCmdArguments(string[] cmdargs)
        {
            CMDArguments objCMDArguments = new CMDArguments();

            try
            {
                if (cmdargs[0] == "-run" & cmdargs.Length == 1)
                {
                    objCMDArguments.bParseCmdArguments = true;
                }
                else
                {
                    objCMDArguments.bParseCmdArguments = false;
                }
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                objCMDArguments.bParseCmdArguments = false;
            }

            return objCMDArguments;
        }

        static AccountLoginParams funcParseConfigFile(CMDArguments objCMDArguments2)
        {
            AccountLoginParams newParams = new AccountLoginParams();

            try
            {
                newParams.lstExclude = new List<string>();
                newParams.lstExcludePrefix = new List<string>();
                newParams.lstExcludeSuffix = new List<string>();

                TextReader trConfigFile = new StreamReader("configAccountLogonCheck.txt");

                newParams.intDaysSinceLastLogon = 0; // initialize
                newParams.intDaysSinceDateCreated = 0; // initialize
                newParams.strDisableUnusedAccounts = "no"; // initialize
                newParams.strUseExclusionGroups = "no"; // initialize

                // setup automatic exclusions
                newParams.lstExcludePrefix.Add("SYSTEMMAILBOX");

                newParams.lstExclude.Add("ADMINISTRATOR");
                newParams.lstExclude.Add("GUEST");
                newParams.lstExclude.Add("KRBTGT");
                newParams.lstExclude.Add("SUPPORT_388945A0");

                using (trConfigFile)
                {
                    string strNewLine = "";

                    while ((strNewLine = trConfigFile.ReadLine()) != null)
                    {

                        //if (strNewLine.StartsWith("DisabledUsersLocation=") & strNewLine != "DisabledUsersLocation=")
                        //{
                        //    newParams.strDisabledUsersLocation = strNewLine.Substring(22);
                        //    //[DebugLine] Console.WriteLine(newParams.strDisabledUsersLocation);
                        //}
                        if (strNewLine.StartsWith("UseExclusionGroups=") & strNewLine != "UseExclusionGroups=")
                        {
                            newParams.strDisableUnusedAccounts = strNewLine.Substring(19);
                            //[DebugLine] Console.WriteLine(strNewLine.Substring(22) + newParams.strDisableUnusedAccounts);
                        }
                        if (strNewLine.StartsWith("ExcludePrefix=") & strNewLine != "ExcludePrefix=")
                        {
                            if (!newParams.lstExcludePrefix.Contains(strNewLine.Substring(14).ToUpper()))
                            {
                                newParams.lstExcludePrefix.Add(strNewLine.Substring(14).ToUpper());
                                //[DebugLine] Console.WriteLine(strNewLine.Substring(14));
                            }
                        }
                        if (strNewLine.StartsWith("ExcludeSuffix=") & strNewLine != "ExcludeSuffix=")
                        {
                            if (!newParams.lstExcludeSuffix.Contains(strNewLine.Substring(14).ToUpper()))
                            {
                                newParams.lstExcludeSuffix.Add(strNewLine.Substring(14).ToUpper());
                                //[DebugLine] Console.WriteLine(strNewLine.Substring(14));
                            }
                        }
                        if (strNewLine.StartsWith("Exclude=") & strNewLine != "Exclude=")
                        {
                            if (!newParams.lstExclude.Contains(strNewLine.Substring(8).ToUpper()))
                            {
                                newParams.lstExclude.Add(strNewLine.Substring(8).ToUpper());
                                //[DebugLine] Console.WriteLine(strNewLine.Substring(8));
                            }
                        }
                        if (strNewLine.StartsWith("DaysSinceLastLogon=") & strNewLine != "DaysSinceLastLogon=")
                        {
                            newParams.intDaysSinceLastLogon = Int32.Parse(strNewLine.Substring(19));
                            //[DebugLine] Console.WriteLine(strNewLine.Substring(19) + newParams.intDaysSinceLastLogon.ToString());
                        }
                        if (strNewLine.StartsWith("DaysSinceDateCreated=") & strNewLine != "DaysSinceDateCreated=")
                        {
                            newParams.intDaysSinceDateCreated = Int32.Parse(strNewLine.Substring(21));
                            //[DebugLine] Console.WriteLine(strNewLine.Substring(21) + newParams.intDaysSinceDateCreated.ToString());
                        }
                        if (strNewLine.StartsWith("DisableUnusedAccounts=") & strNewLine != "DisableUnusedAccounts=")
                        {
                            newParams.strDisableUnusedAccounts = strNewLine.Substring(22);
                            //[DebugLine] Console.WriteLine(strNewLine.Substring(22) + newParams.strDisableUnusedAccounts);
                        }
                    }
                }

                //[DebugLine] Console.WriteLine("# of Exclude= : {0}", newParams.lstExclude.Count.ToString());
                //[DebugLine] Console.WriteLine("# of ExcludePrefix= : {0}", newParams.lstExcludePrefix.Count.ToString());

                trConfigFile.Close();

                if (newParams.intDaysSinceLastLogon == 0)
                {
                    newParams.intDaysSinceLastLogon = 21;
                }
                else
                {
                    newParams.intDaysSinceLastLogon = newParams.intDaysSinceLastLogon + 14; // 14-day lastLogonTimestamp update window (9-14 days)
                }

                if (newParams.intDaysSinceDateCreated == 0)
                {
                    newParams.intDaysSinceDateCreated = 7; // set to 7 days as default if not specified in config file
                }


            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }

            return newParams;
        }

        static void funcProgramExecution(CMDArguments objCMDArguments2)
        {
            try
            {
                // [DebugLine] Console.WriteLine("Entering funcProgramExecution");
                if (funcCheckForFile("configAccountLogonCheck.txt"))
                {

                    funcToEventLog("AccountLogonCheck", "AccountLogonCheck started", 100);

                    funcProgramRegistryTag("AccountLogonCheck");

                    // Open a TextWriter to pass to each function for logging 
                    // The TextWriter was created at this level to help with
                    // log file formatting
                    TextWriter twCurrent = funcOpenOutputLog();
                    //twCurrent.WriteLine("Date\tMessage");

                    funcWriteToOutputLog(twCurrent, "--------AccountLogonCheck started");

                    AccountLoginParams newParams = funcParseConfigFile(objCMDArguments2);

                    // Find enabled accounts that have not logged in since
                    // the specified period and disable
                    funcCheckLogin(twCurrent, newParams);

                    // Find enabled, unused accounts that have not been
                    // used within the specified period and disable if specified
                    funcFindUnusedAccounts(twCurrent, newParams);

                    funcWriteToOutputLog(twCurrent, "--------AccountLogonCheck stopped");

                    // Close the TextWriter that was opened for logging
                    funcCloseOutputLog(twCurrent);

                    funcToEventLog("AccountLogonCheck", "AccountLogonCheck stopped", 101);
                }
                else
                {
                    Console.WriteLine("Config file configAccountLogonCheck.txt could not be found.");
                }

            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }

        }

        static void funcProgramRegistryTag(string strProgramName)
        {
            try
            {
                string strRegistryProfilesPath = "SOFTWARE";
                RegistryKey objRootKey = Microsoft.Win32.Registry.LocalMachine;
                RegistryKey objSoftwareKey = objRootKey.OpenSubKey(strRegistryProfilesPath, true);
                RegistryKey objSystemsAdminProKey = objSoftwareKey.OpenSubKey("SystemsAdminPro", true);
                if (objSystemsAdminProKey == null)
                {
                    objSystemsAdminProKey = objSoftwareKey.CreateSubKey("SystemsAdminPro");
                }
                if (objSystemsAdminProKey != null)
                {
                    if (objSystemsAdminProKey.GetValue(strProgramName) == null)
                        objSystemsAdminProKey.SetValue(strProgramName, "1", RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static DirectorySearcher funcCreateDSSearcher()
        {
            try
            {
                System.DirectoryServices.DirectorySearcher objDSSearcher = new DirectorySearcher();
                // [Comment] Get local domain context

                string rootDSE;

                System.DirectoryServices.DirectorySearcher objrootDSESearcher = new System.DirectoryServices.DirectorySearcher();
                rootDSE = objrootDSESearcher.SearchRoot.Path;
                //Console.WriteLine(rootDSE);

                // [Comment] Construct DirectorySearcher object using rootDSE string
                System.DirectoryServices.DirectoryEntry objrootDSEentry = new System.DirectoryServices.DirectoryEntry(rootDSE);
                objDSSearcher = new System.DirectoryServices.DirectorySearcher(objrootDSEentry);
                //Console.WriteLine(objDSSearcher.SearchRoot.Path);

                return objDSSearcher;
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return null;
            }
        }

        static PrincipalContext funcCreatePrincipalContext()
        {
            PrincipalContext newctx = new PrincipalContext(ContextType.Machine);

            try
            {
                //Console.WriteLine("Entering funcCreatePrincipalContext");
                Domain objDomain = Domain.GetComputerDomain();
                string strDomain = objDomain.Name;
                DirectorySearcher tempDS = funcCreateDSSearcher();
                string strDomainRoot = tempDS.SearchRoot.Path.Substring(7);
                // [DebugLine] Console.WriteLine(strDomainRoot);
                // [DebugLine] Console.WriteLine(strDomainRoot);

                newctx = new PrincipalContext(ContextType.Domain,
                                    strDomain,
                                    strDomainRoot);

                // [DebugLine] Console.WriteLine(newctx.ConnectedServer);
                // [DebugLine] Console.WriteLine(newctx.Container);



                //if (strContextType == "Domain")
                //{

                //    PrincipalContext newctx = new PrincipalContext(ContextType.Domain,
                //                                    strDomain,
                //                                    strDomainRoot);
                //    return newctx;
                //}
                //else
                //{
                //    PrincipalContext newctx = new PrincipalContext(ContextType.Machine);
                //    return newctx;
                //}
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }

            if (newctx.ContextType == ContextType.Machine)
            {
                Exception newex = new Exception("The Active Directory context did not initialize properly.");
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, newex);
            }

            return newctx;
        }

        static bool funcCheckNameExclusion(string strName, AccountLoginParams listParams)
        {
            try
            {
                bool bNameExclusionCheck = false;

                // automatic exclusions are set in funcParseConfigFile
                if (listParams.strUseExclusionGroups == "yes")
                {
                    Domain dmCurrent = Domain.GetCurrentDomain();
                    //[DebugLine] Console.WriteLine(dmCurrent.Name);
                    PrincipalContext ctxDomain = new PrincipalContext(ContextType.Domain, dmCurrent.Name);
                    //[DebugLine] Console.WriteLine(ctxDomain.ConnectedServer + "\t" + ctxDomain.Container + "\t" + ctxDomain.Name);
                    GroupPrincipal grpOWALogonExclusions = GroupPrincipal.FindByIdentity(ctxDomain, IdentityType.SamAccountName, "OWALogonExclusions");
                    GroupPrincipal grpServiceAccountExclusions = GroupPrincipal.FindByIdentity(ctxDomain, IdentityType.SamAccountName, "ServiceAccountExclusions");
                    UserPrincipal upTemp = UserPrincipal.FindByIdentity(ctxDomain, IdentityType.SamAccountName, strName);
                    
                    if(grpOWALogonExclusions != null & upTemp != null)
                    {
                        if (upTemp.IsMemberOf(grpOWALogonExclusions))
                            bNameExclusionCheck = true;
                    }
                    if (grpServiceAccountExclusions != null & upTemp != null)
                    {
                        if (upTemp.IsMemberOf(grpServiceAccountExclusions))
                            bNameExclusionCheck = true;
                    }

                }

                strName = strName.ToUpper();

                if (listParams.lstExclude.Contains(strName))
                    bNameExclusionCheck = true;

                foreach (string strNameTemp in listParams.lstExcludePrefix)
                {
                    if (strName.StartsWith(strNameTemp))
                    {
                        bNameExclusionCheck = true;
                        break;
                    }
                }

                foreach (string strNameTemp in listParams.lstExcludeSuffix)
                {
                    if (strName.EndsWith(strNameTemp))
                    {
                        bNameExclusionCheck = true;
                        break;
                    }
                }

                return bNameExclusionCheck;
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return false;
            }
        }

        static string funcGetLastLogonTimestamp(DirectoryEntry tmpDE)
        {
            try
            {
                string strTimestamp = String.Empty;

                if (tmpDE.Properties.Contains("lastLogonTimestamp"))
                {
                    //[DebugLine] Console.WriteLine(u.Name + " has lastLogonTimestamp attribute");
                    IADsLargeInteger lintLogonTimestamp = (IADsLargeInteger)tmpDE.Properties["lastLogonTimestamp"].Value;
                    if (lintLogonTimestamp != null)
                    {
                        DateTime dtLastLogonTimestamp = funcGetDateTimeFromLargeInteger(lintLogonTimestamp);
                        if (dtLastLogonTimestamp != null)
                        {
                            strTimestamp = dtLastLogonTimestamp.ToLocalTime().ToString();
                        }
                        else
                        {
                            strTimestamp = "(null)";
                        }
                    }
                }

                return strTimestamp;
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return String.Empty;
            }
        }

        static string funcGetAccountCreationDate(DirectoryEntry tmpDE)
        {
            try
            {
                string strCreationDate = String.Empty;

                if (tmpDE.Properties.Contains("whenCreated"))
                {
                    strCreationDate = (string)tmpDE.Properties["whenCreated"].Value.ToString();
                }

                return strCreationDate;
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return String.Empty;
            }
        }

        static void funcCheckLogin(TextWriter twCurrent, AccountLoginParams currentParams)
        {
            try
            {
                string strAccountSAMName = "";
                string strlastLogonTimestamp = ""; //lastLogonTimestamp attribute
                string strOutputMsg = "";

                PrincipalContext ctx = funcCreatePrincipalContext();
                DateTime dtFilter = DateTime.Today.AddDays(-currentParams.intDaysSinceLastLogon);

                string strQueryFilter = "(&(objectCategory=person)(objectClass=user)(lastLogonTimestamp<=" +
                                        dtFilter.ToLocalTime().ToFileTime().ToString() +
                                        ")(!userAccountControl:1.2.840.113556.1.4.803:=2))";

                DirectorySearcher dsLoginCheck = new DirectorySearcher(strQueryFilter);
                //[DebugLine] Console.WriteLine(dsLoginCheck.SearchRoot.Path);

                SearchResultCollection srcLoginCheck = dsLoginCheck.FindAll();
                //[DebugLine] Console.WriteLine(srcLoginCheck.Count.ToString());
                funcToEventLog("AccountLogonCheck", "Number of accounts with no logon since " +
                               dtFilter.ToLocalTime().ToString("MM/dd/yyyy") +
                               " to process: " + srcLoginCheck.Count.ToString(), 1001);

                if (srcLoginCheck.Count > 0)
                {
                    foreach (SearchResult sr in srcLoginCheck)
                    {
                        DirectoryEntry tmpDE = sr.GetDirectoryEntry();

                        strAccountSAMName = tmpDE.Properties["sAMAccountName"].Value.ToString();
                        strlastLogonTimestamp = funcGetLastLogonTimestamp(tmpDE);

                        tmpDE.Close();

                        if (!funcCheckNameExclusion(strAccountSAMName, currentParams))
                        {
                            UserPrincipal newUserPrincipal = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, strAccountSAMName);

                            newUserPrincipal.Enabled = false;
                            newUserPrincipal.Save();
                            if (newUserPrincipal.Enabled == false)
                            {
                                strOutputMsg = "Last login: " + newUserPrincipal.Name + " - " + strlastLogonTimestamp + "\t(Action: Disabled)";
                            }
                            else
                            {
                                strOutputMsg = "Last login: " + newUserPrincipal.Name + " - " + strlastLogonTimestamp + "\t(Action: NotDisabled-Check account)";
                            }

                            funcWriteToOutputLog(twCurrent, strOutputMsg);
                        }

                    }
                }

                srcLoginCheck.Dispose();
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static void funcFindUnusedAccounts(TextWriter twCurrent, AccountLoginParams currentParams)
        {
            try
            {
                string strAccountSAMName = "";
                string strOutputMsg = "";
                string strWhenCreated = "";

                PrincipalContext ctx = funcCreatePrincipalContext();

                DateTime dtWhenCreatedCutOff = DateTime.Today.AddDays(-currentParams.intDaysSinceDateCreated);

                string strQueryFilter = "(&(objectCategory=person)(objectClass=user)(!lastLogonTimestamp=*)" +
                                        "(!userAccountControl:1.2.840.113556.1.4.803:=2))";

                DirectorySearcher dsLoginCheck = new DirectorySearcher(strQueryFilter);
                //[DebugLine] Console.WriteLine(dsLoginCheck.SearchRoot.Path);

                SearchResultCollection srcLoginCheck = dsLoginCheck.FindAll();
                //[DebugLine] Console.WriteLine(srcLoginCheck.Count.ToString());
                funcToEventLog("AccountLogonCheck", "Number of unused accounts to process: " + srcLoginCheck.Count.ToString(), 1002);

                if (srcLoginCheck.Count > 0)
                {
                    foreach (SearchResult sr in srcLoginCheck)
                    {
                        DirectoryEntry tmpDE = sr.GetDirectoryEntry();

                        strAccountSAMName = tmpDE.Properties["sAMAccountName"].Value.ToString();
                        strWhenCreated = funcGetAccountCreationDate(tmpDE);

                        tmpDE.Close();

                        DateTime dtWhenCreated = Convert.ToDateTime(strWhenCreated);
                        //[DebugLine] Console.WriteLine(dtWhenCreated.ToString());

                        if (!funcCheckNameExclusion(strAccountSAMName, currentParams))
                        {
                            UserPrincipal newUserPrincipal = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, strAccountSAMName);

                            if (dtWhenCreated < dtWhenCreatedCutOff)
                            {
                                if (currentParams.strDisableUnusedAccounts == "yes")
                                {
                                    newUserPrincipal.Enabled = false;
                                    newUserPrincipal.Save();
                                    if (newUserPrincipal.Enabled == false)
                                    {
                                        strOutputMsg = "Unused account: " + newUserPrincipal.Name + "\t(Action: Disabled)";
                                    }
                                    else
                                    {
                                        strOutputMsg = "Unused account: " + newUserPrincipal.Name + "\t(Action: NotDisabled-Check account)";
                                    }
                                }
                                else
                                {
                                    strOutputMsg = "Unused account: " + newUserPrincipal.Name + "\t(NoAction: Outside allowed no-use period)";
                                }
                            }
                            else
                            {
                                strOutputMsg = "Unused account: " + newUserPrincipal.Name + "\t(NoAction: Inside allowed no-use period)";
                            }

                            funcWriteToOutputLog(twCurrent, strOutputMsg);
                        }

                    }
                }

                srcLoginCheck.Dispose();
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static void funcToEventLog(string strAppName, string strEventMsg, int intEventType)
        {
            try
            {
                string strLogName;

                strLogName = "Application";

                if (!EventLog.SourceExists(strAppName))
                    EventLog.CreateEventSource(strAppName, strLogName);

                //EventLog.WriteEntry(strAppName, strEventMsg);
                EventLog.WriteEntry(strAppName, strEventMsg, EventLogEntryType.Information, intEventType);
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static bool funcCheckForOU(string strOUPath)
        {
            try
            {
                string strDEPath = "";

                if (!strOUPath.Contains("LDAP://"))
                {
                    strDEPath = "LDAP://" + strOUPath;
                }
                else
                {
                    strDEPath = strOUPath;
                }

                if (DirectoryEntry.Exists(strDEPath))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return false;
            }
        }

        static bool funcCheckForFile(string strInputFileName)
        {
            try
            {
                if (System.IO.File.Exists(strInputFileName))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return false;
            }
        }

        static void funcGetFuncCatchCode(string strFunctionName, Exception currentex)
        {
            string strCatchCode = "";

            Dictionary<string, string> dCatchTable = new Dictionary<string, string>();
            dCatchTable.Add("funcCheckForFile", "f0");
            dCatchTable.Add("funcCheckForOU", "f1");
            dCatchTable.Add("funcCheckLogon", "f2");
            dCatchTable.Add("funcCheckNameExclusion", "f3");
            dCatchTable.Add("funcCloseOutputLog", "f4");
            dCatchTable.Add("funcCreateDSSearcher", "f5");
            dCatchTable.Add("funcCreatePrincipalContext", "f6");
            dCatchTable.Add("funcErrorToEventLog", "f7");
            dCatchTable.Add("funcFindUnusedAccounts", "f8");
            dCatchTable.Add("funcGetAccountCreationDate", "f9");
            dCatchTable.Add("funcGetDateTimeFromLargeInteger", "f10");
            dCatchTable.Add("funcGetFuncCatchCode", "f11");
            dCatchTable.Add("funcGetLastLogonTimestamp", "f12");
            dCatchTable.Add("funcLicenseActivation", "f13");
            dCatchTable.Add("funcLicenseCheck", "f14");
            dCatchTable.Add("funcOpenOutputLog", "f15");
            dCatchTable.Add("funcParseCmdArguments", "f16");
            dCatchTable.Add("funcParseConfigFile", "f17");
            dCatchTable.Add("funcPrintParameterSyntax", "f18");
            dCatchTable.Add("funcPrintParameterWarning", "f19");
            dCatchTable.Add("funcProgramExecution", "f20");
            dCatchTable.Add("funcProgramRegistryTag", "f21");
            dCatchTable.Add("funcToEventLog", "f22");
            dCatchTable.Add("funcWriteToErrorLog", "f23");
            dCatchTable.Add("funcWriteToOutputLog", "f24");

            if (dCatchTable.ContainsKey(strFunctionName))
            {
                strCatchCode = "err" + dCatchTable[strFunctionName] + ": ";
            }

            //[DebugLine] Console.WriteLine(strCatchCode + currentex.GetType().ToString());
            //[DebugLine] Console.WriteLine(strCatchCode + currentex.Message);

            funcWriteToErrorLog(strCatchCode + currentex.GetType().ToString());
            funcWriteToErrorLog(strCatchCode + currentex.Message);
            funcErrorToEventLog("AccountLogonCheck");

        }

        static void funcWriteToErrorLog(string strErrorMessage)
        {
            try
            {
                string strPath = Directory.GetCurrentDirectory();

                if (!Directory.Exists(strPath + "\\Log"))
                {
                    Directory.CreateDirectory(strPath + "\\Log");
                    if (Directory.Exists(strPath + "\\Log"))
                    {
                        strPath = strPath + "\\Log";
                    }
                }
                else
                {
                    strPath = strPath + "\\Log";
                }

                FileStream newFileStream = new FileStream(strPath + "\\Err-AccountLogonCheck.log", FileMode.Append, FileAccess.Write);
                TextWriter twErrorLog = new StreamWriter(newFileStream);

                DateTime dtNow = DateTime.Now;

                string dtFormat = "MMddyyyy HH:mm:ss";

                twErrorLog.WriteLine("{0}\t{1}", dtNow.ToLocalTime().ToString(dtFormat), strErrorMessage);

                twErrorLog.Close();
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }

        }

        static void funcErrorToEventLog(string strAppName)
        {
            string strLogName;

            strLogName = "Application";

            if (!EventLog.SourceExists(strAppName))
                EventLog.CreateEventSource(strAppName, strLogName);

            //EventLog.WriteEntry(strAppName, strEventMsg);
            EventLog.WriteEntry(strAppName, "An error has occured. Check log file.", EventLogEntryType.Error, 0);
        }

        static TextWriter funcOpenOutputLog()
        {
            try
            {
                DateTime dtNow = DateTime.Now;

                string dtFormat2 = "MMddyyyy"; // for log file directory creation

                string strPath = Directory.GetCurrentDirectory();

                if (!Directory.Exists(strPath + "\\Log"))
                {
                    Directory.CreateDirectory(strPath + "\\Log");
                    if (Directory.Exists(strPath + "\\Log"))
                    {
                        strPath = strPath + "\\Log";
                    }
                }
                else
                {
                    strPath = strPath + "\\Log";
                }

                string strLogFileName = strPath + "\\AccountLogonCheck" + dtNow.ToLocalTime().ToString(dtFormat2) + ".log";

                FileStream newFileStream = new FileStream(strLogFileName, FileMode.Append, FileAccess.Write);
                TextWriter twOuputLog = new StreamWriter(newFileStream);

                return twOuputLog;
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return null;
            }

        }

        static void funcWriteToOutputLog(TextWriter twCurrent, string strOutputMessage)
        {
            try
            {
                DateTime dtNow = DateTime.Now;

                //string dtFormat = "MM/dd/yyyy";
                string dtFormat2 = "MM/dd/yyyy HH:mm";
                // string dtFormat3 = "MM/dd/yyyy HH:mm:ss";

                twCurrent.WriteLine("{0}\t{1}", dtNow.ToLocalTime().ToString(dtFormat2), strOutputMessage);
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static void funcCloseOutputLog(TextWriter twCurrent)
        {
            try
            {
                twCurrent.Close();
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static DateTime funcGetDateTimeFromLargeInteger(IADsLargeInteger largeIntValue)
        {
                //
                // Convert large integer to int64 value
                //
                long int64Value = (long)((uint)largeIntValue.LowPart +
                         (((long)largeIntValue.HighPart) << 32));

                //
                // Return the DateTime in utc
                //
                // return DateTime.FromFileTimeUtc(int64Value);


                // return in Localtime
                return DateTime.FromFileTime(int64Value);
        }

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    funcPrintParameterWarning();
                }
                else
                {
                    if (args[0] == "-?")
                    {
                        funcPrintParameterSyntax();
                    }
                    else
                    {
                        string[] arrArgs = args;
                        CMDArguments objArgumentsProcessed = funcParseCmdArguments(arrArgs);

                        if (objArgumentsProcessed.bParseCmdArguments)
                        {
                            funcProgramExecution(objArgumentsProcessed);
                        }
                        else
                        {
                            funcPrintParameterWarning();
                        } // check objArgumentsProcessed.bParseCmdArguments
                    } // check args[0] = "-?"
                } // check args.Length == 0
            }
            catch (Exception ex)
            {
                Console.WriteLine("errm0: {0}", ex.Message);
            }
        }
    }
}
