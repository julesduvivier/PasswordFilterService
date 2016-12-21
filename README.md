# PasswordFilterService

## Introduction

PasswordFilterService is a password policy enforcement tool for Windows Active Directory.

Windows has a basic password complexity rule but no good controls to enforce the use of reasonable passwords. 
This basic policy accepts many weak password like `Password1` or `Company2017`

PasswordFilterService checks new passwords for compliance with your custom password policy and rejects non-compliant passwords.

## Getting Started

PasswordFilterService.exe is a C#-based binary service that provide a simple UI to manage your password policy. The service embedded the password filter DLL for 64 and 32 bit system.

The password filter DLL is coded in C and loaded by LSASS on boot and will be queried every time a users try to change his password. For further information check the github project at : https://github.com/julesduvivier/PasswordFilter


Download the last version of the executable from the release :  https://github.com/julesduvivier/PasswordFilterService/releases/latest.

When you first run the PasswordFilterService.exe, the binary will :
- Extract the embedded DLL corresponding to your system and copy it to `%WINDIR%\System32`. 
- Edit the `HKLM\SYSTEM\CurrentControlSet\Control\Lsa\Notification Packages` registry key to add the DLL name.
- Create `HKLM\SOFTWARE\Wow6432Node\PasswordFilter` registry key with your custom pasword policy rules.

Then you just need to reboot your DC to start using your password policy.

## Rules

PasswordFilterService.exe provide you a simple interface to custom your rules : 

![Password Filter Service](https://cloud.githubusercontent.com/assets/7902643/21147685/b9b41d28-c14d-11e6-8231-ac20309b9945.png)

#### Length
The Length rule rejects passwords that contain too few characters.

#### Complexity 
the Complexity rule rejects passwords that do not contain characters from a variety of character sets like lower, upper, digit and special characters 


#### Consecutive letters
The Consecutive letters rule rejects password that contains too many consecutive characters

#### Log file
The path where the log file will be written.

The PasswordFilter function is implemented by the PasswordFilter DLL. This function simply replies with a TRUE or FALSE, as appropriate, to indicate that the password passes or fails the test. 
Thereby, it's impossible to return the error message directly to the user so the log file allow the admin of the domain to know the reason of a password rejection.

Example of log file :
```
29/11/16 03:41PM [Username] - The password doesn't meet the complexity requirements : It must contain at least one uppercase letter 
30/11/16 06:12PM [Username] - The password doesn't meet the complexity requirements : It must contain at least one special letter 
11/12/16 11:42PM [Username] - Password contains banned word : Company
11/12/16 11:43PM [Username] - Password can't exceed 3 consecutive characters
```

#### Wordlist
The Wordlist rule rejects password which contains a word from the dictonary. Your dictonary must contain one forbidden word per line.
There are many tools to generate custom wordlist (e.g. based on your company name) 

Example of wordlist : 
```
Company
COMPANY
c0mp4ny
Password
Passw0rd
...
```

The password `PasswordMay2016` will be rejected because it contained the substring `Password` which is in the wordlist

#### Tokens wordlist
The Tokens wordlist rule rejects password's token which match perfectly with this second dictionnary. The password is tokenized based on the change of the characters types and each tokens are compared 
with the tokens wordlist.

Example of Tokens wordlist :
```
123
May
2016
...
```

The password `PasswordMay1` will be rejected because it contained the substring `May` but `PasswordMaya1` will not be rejected because the token `Maya` isn't in the Token wordlist.
