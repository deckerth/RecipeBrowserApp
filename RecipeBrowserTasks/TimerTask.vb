Imports System
Imports System.Diagnostics
Imports System.Threading
Imports Windows.ApplicationModel.Background
Imports Windows.Foundation
Imports Windows.Storage
Imports Windows.System.Threading

Namespace Global.RecipeBrowserTasks

    Public NotInheritable Class TimerTask
        Implements IBackgroundTask

        Dim _periodicTimer As ThreadPoolTimer = Nothing
        Dim _taskInstance As IBackgroundTaskInstance = Nothing
        Dim _deferral As BackgroundTaskDeferral = Nothing

        Dim timerStart As DateTime

        Public Sub Run(taskInstance As IBackgroundTaskInstance) Implements IBackgroundTask.Run

            _deferral = taskInstance.GetDeferral()
            _taskInstance = taskInstance
            timerStart = DateTime.Now
            _periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(New TimerElapsedHandler(AddressOf PeriodicTimerCallback), TimeSpan.FromSeconds(1))

        End Sub

        Private Sub PeriodicTimerCallback(timer As ThreadPoolTimer)
            Dim now = DateTime.Now

            Dim diff = now.Subtract(timerStart)
            _taskInstance.Progress = diff.Seconds

        End Sub

    End Class

End Namespace
