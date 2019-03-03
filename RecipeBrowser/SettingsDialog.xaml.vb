' Die Elementvorlage "Inhaltsdialog" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Public NotInheritable Class SettingsDialog
    Inherits ContentDialog

    Public Sub New()

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()

        ' Fügen Sie Initialisierungen nach dem InitializeComponent()-Aufruf hinzu.
        LoggingEnabledSwitch.IsOn = App.Logger.IsEnabled

        Select Case App.SelectedApplicationTheme
            Case App.ApplicationThemeDark
                ThemeComboBox.SelectedItem = ThemeDark
            Case App.ApplicationThemeLight
                ThemeComboBox.SelectedItem = ThemeLight
        End Select
    End Sub

    Private Async Sub ContentDialog_PrimaryButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)

        Dim settings = Windows.Storage.ApplicationData.Current.LocalSettings
        Dim themeChanged As Boolean = False

        Select Case ThemeComboBox.SelectedIndex
            Case 0 ' Light
                If App.SelectedApplicationTheme = App.ApplicationThemeDark Then
                    settings.Values("ApplicationTheme") = App.ApplicationThemeLight
                    themeChanged = True
                End If
            Case 1 'Dark
                If App.SelectedApplicationTheme = App.ApplicationThemeLight Then
                    settings.Values("ApplicationTheme") = App.ApplicationThemeDark
                    themeChanged = True
                End If
        End Select

        If themeChanged Then
            Dim msg = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("RestartAppForThemeChangeRequired"))
            Await msg.ShowAsync()
        End If

        Hide()
    End Sub

    Private Sub ChangeRootFolder_Click(sender As Object, e As RoutedEventArgs) Handles ChangeRootFolder.Click
        CategoryOverview.Current.ChangeRootFolderRequested = True
        Hide()
    End Sub

    Private Sub LoggingEnabledSwitch_Toggled(sender As Object, e As RoutedEventArgs) Handles LoggingEnabledSwitch.Toggled
        App.Logger.SetActive(LoggingEnabledSwitch.IsOn)
    End Sub
End Class
