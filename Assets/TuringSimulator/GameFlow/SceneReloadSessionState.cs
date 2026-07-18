namespace TuringSimulator.GameFlow
{
    internal static class SceneReloadSessionState
    {
        private static string _preservedStudentId;

        public static void PreserveStudent(string studentId)
        {
            _preservedStudentId = studentId;
        }

        public static bool TryConsumeStudent(out string studentId)
        {
            studentId = _preservedStudentId;
            _preservedStudentId = null;
            return !string.IsNullOrWhiteSpace(studentId);
        }
    }
}
