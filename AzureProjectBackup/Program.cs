namespace AzureProjectBackup
{
    using System.Text.RegularExpressions;

    internal class Program
    {
        static async Task Main(string[] args)
        {
            string copyToProject = "";
            string copyFromProject = "";
            string azureOrganization = "";
            string personalAccessToken = "";
            string[] inputArray = args;
            while (!ParseInput(
                inputArray,
                ref copyToProject,
                ref copyFromProject,
                ref azureOrganization,
                ref personalAccessToken))
            {
                Console.WriteLine("Enter following items: [Target project] [Source project] [Azure organization] [Personal access token]");
                string? input = Console.ReadLine();
                if (input == null)
                {
                    continue;
                }
                inputArray = input.Split(' ');
            }

            BackupCreator copier = new();
            await copier.CreateBackup(copyToProject, copyFromProject, azureOrganization, personalAccessToken);
        }

        static bool ParseInput(
            string[] inputArray,
            ref string copyToProject,
            ref string copyFromProject,
            ref string azureOrganization,
            ref string personalAccessToken)
        {
            if (inputArray.Length != 4)
            {
                return false;
            }
            foreach (string input in inputArray)
            {
                if (!IsValidString(input))
                {
                    Console.WriteLine($"Invalid input [{input}]: String must start with alphabet character " +
                                      "and contain only alphanumeric characters, hyphen (-), or underscore (_)");
                    return false;
                }
            }
            copyToProject = inputArray[0];
            copyFromProject = inputArray[1];
            azureOrganization = inputArray[2];
            personalAccessToken = inputArray[3];
            return true;
        }

        // Program input string is valid if it starts with alphabet character and
        // contains alphanumeric characters, hyphen (-), or underscore (_)
        static bool IsValidString(string input)
        {
            const string RegEx = "^[a-zA-Z][a-zA-Z0-9-_]*$";
            return Regex.Match(input, RegEx).Success;
        }
    }
}
