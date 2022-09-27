// See https://aka.ms/new-console-template for more information
using Microsoft.Win32;
using System.IO;

Console.WriteLine("Hello, World :D !");



string[] rebootRequiredKeys = {
            @"Software\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending" ,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired",
            @"SYSTEM\CurrentControlSet\Control\Session Manager"
};



