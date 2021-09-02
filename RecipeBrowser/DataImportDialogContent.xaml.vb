' Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Imports RecipeBrowser.Common
''' <summary>
''' Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
''' </summary>
Public NotInheritable Class DataImportDialogContent
    Inherits Page

    Public Enum ContentMode
        Metadata
        History
    End Enum

    Public Enum ImportMode
        Add
        Replace
    End Enum

    Public Property DialogMode As ContentMode
    Public Shared ReadOnly Property MetadataMode As ContentMode
        Get
            Return ContentMode.Metadata
        End Get
    End Property

    Public Shared ReadOnly Property HistoryMode As ContentMode
        Get
            Return ContentMode.History
        End Get
    End Property

    Public Property ImportDbCaloricInfos As Integer
    Public Property ImportDbRecipeTags As Integer
    Public Property ImportDbTagDirectoryEntries As Integer
    Public Property ImportDbHistoryEntries As Integer

    Public Property CurrentDbCaloricInfos As Integer
    Public Property CurrentDbRecipeTags As Integer
    Public Property CurrentDbTagDirectoryEntries As Integer
    Public Property CurrentDbHistoryEntries As Integer

    Public Shared Function VisibleIfEquals(mode As ContentMode, compareMode As ContentMode) As Visibility
        Return If(mode = compareMode, Visibility.Visible, Visibility.Collapsed)
    End Function

    Public Property Cancelled As Boolean = False
    Public Property SelectedMode As ImportMode

    Public Property CloseButtonCommand As RelayCommand
    Public Property PrimaryButtonCommand As RelayCommand
    Public Property SecondaryButtonCommand As RelayCommand

    Private Sub CancelClicked()
        Cancelled = True
    End Sub

    Private Sub PrimaryButtonClicked()
        SelectedMode = ImportMode.Add
    End Sub

    Private Sub SecondaryButtonClicked()
        SelectedMode = ImportMode.Replace
    End Sub

    Public Sub New()
        InitializeComponent()
        CloseButtonCommand = New RelayCommand(AddressOf CancelClicked)
        PrimaryButtonCommand = New RelayCommand(AddressOf PrimaryButtonClicked)
        SecondaryButtonCommand = New RelayCommand(AddressOf SecondaryButtonClicked)
    End Sub

End Class
