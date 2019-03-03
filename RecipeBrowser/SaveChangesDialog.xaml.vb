' Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Public NotInheritable Class SaveChangesDialog
    Inherits ContentDialog

    Public Enum UserChoices
        Yes
        No
        Cancel
    End Enum

    Public UserChoice As UserChoices = UserChoices.Cancel

    Private Sub SaveDialog_PrimaryButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)
        UserChoice = UserChoices.Yes
    End Sub

    Private Sub SaveDialog_SecondaryButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)
        UserChoice = UserChoices.No
    End Sub

End Class
