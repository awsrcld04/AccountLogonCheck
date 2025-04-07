
# AccountLogonCheck

DESCRIPTION: Find and disable user accounts that are no longer being used


## REQUIREMENTS:

Operating System Requirements:
- Windows Server 2003 or higher (32-bit)
- Windows Server 2008 or higher (32-bit)

Additional software requirements:
Microsoft .NET Framework v3.5

Active Directory requirements:
One of following domain functional levels
- Windows Server 2003 domain functional level
- Windows Server 2008 domain functional level

Additional requirements:
Domain administrative access is required to perform operations by AccountLogonCheck


## Operation and Configuration:

Command-line parameters:
-run (Required parameter)

Configuration file: configAccountLogonCheck.txt
- Located in the same directory as AccountLogonCheck.exe

Configuration file parameters:

DaysSinceDateCreated: Number of days to use to exclude accounts that may have been recently created
- This parameter is only used if the DisableUnusedAccounts parameter is set to yes
- If not specified and the DisabledUnusedAccounts parameter is set to yes, the default is 7 days

DaysSinceLastLogon: Number of days after a 14-day period (2 weeks) to use to search for accounts that are not being used
- For example, specifying 16 days would add up to a total of 30 days
- If not specified, the default is a total of 21 days

DisableUnusedAccounts (yes | no): Determines if AccountLogonCheck will disable accounts that have not been used
- If yes, the DaysSinceDateCreated parameter is used.
- If not specified, the default is no

Exclude: Exclude one or more accounts by specifying the desired account on a separate line

ExcludePrefix: Exclude one or more accounts using a prefix that will match the desired account(s)

ExcludeSuffix: Exclude one or more accounts using a suffix that will match the desired account(s)

Output:
- Located in the Log directory inside the installation directory; log files are in tab-delimited
format
- Path example: (InstallationDirectory)\Log\

Additional detail:
- Accounts are only placed into a disabled state by AccountLogonCheck. No other operations are performed by AccountLogonCheck.
- The DisableUnusedAccounts parameter refers to accounts that do not have the lastLogonTimestamp attribute.
