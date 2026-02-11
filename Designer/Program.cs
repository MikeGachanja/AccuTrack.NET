using Designer.Modules.Project;

namespace Designer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Show startup page
            using var startupPage = new StartupPage();
            if (startupPage.ShowDialog() == DialogResult.Cancel)
            {
                return; // User clicked Cancel, exit application
            }

            // Create main window
            var mainForm = new MainForm();

            // Handle user's choice from startup page
            if (startupPage.ShouldCreateNewProject())
            {
                // Trigger new project creation
                mainForm.NewProject();
            }
            else if (startupPage.ShouldOpenProject())
            {
                // Open the selected project
                string projectPath = startupPage.GetSelectedProjectPath();
                if (!string.IsNullOrEmpty(projectPath))
                {
                    mainForm.OpenProjectPath(projectPath);
                }
            }

            Application.Run(mainForm);
        }
    }
}