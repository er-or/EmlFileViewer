namespace EmlFileViewer {
    internal static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(String[] args) {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.

            System.Text.EncodingProvider provider = System.Text.CodePagesEncodingProvider.Instance;
            System.Text.Encoding.RegisterProvider(provider);

            ApplicationConfiguration.Initialize();
            EmlViewer ev = new EmlViewer();
            if (args.Length > 0) {
                ev.EmlViewer_OpenFile(args[args.Length - 1]);
            }
            Application.Run(ev);
        }
    }
}