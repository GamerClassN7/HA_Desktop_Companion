// See https://aka.ms/new-console-template for more information
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;

Console.WriteLine("Hello, World!");


string name = "Processor";
string category = "% Processor Time";
string instance = "_Total";
int maxSampleCount = 3;

Console.WriteLine(rqueryPerfCounter(name, category, instance, maxSampleCount));
Console.WriteLine();

static float rqueryPerfCounter(string name, string category, string instance, int maxSampleCount)
{
    try
    {
        PerformanceCounter total_cpu = new(name, category, instance);

        maxSampleCount = 3;
        int sampleCount = maxSampleCount;

        float result = 0;
        while (sampleCount >= 0)
        {
            if (sampleCount != maxSampleCount)
            {
                result += total_cpu.NextValue();
            }

            sampleCount--;
        }

        return (result / maxSampleCount);
    }
    catch (Exception)
    {

        return 0.0f;
    }
}