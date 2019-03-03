' Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Public NotInheritable Class ScanForCaloriesProgressDialog
    Inherits ContentDialog
    Implements INotifyPropertyChanged

    Private _currentCategory As RecipeFolder
    Public Property CurrentCategory As RecipeFolder
        Get
            Return _currentCategory
        End Get
        Set(value As RecipeFolder)
            If (_currentCategory Is Nothing AndAlso value IsNot Nothing) OrElse
               (_currentCategory IsNot Nothing AndAlso value Is Nothing) Then
                _currentCategory = value
                OnPropertyChanged("CurrentCategory")
            ElseIf _currentCategory IsNot Nothing AndAlso value IsNot Nothing AndAlso
                   _currentCategory.Name <> value.Name Then
                _currentCategory = value
                OnPropertyChanged("CurrentCategory")
            End If
        End Set
    End Property

    Private CategoryToScan As RecipeFolder = Nothing

    Public Sub SetCategory(Category As RecipeFolder)
        CategoryToScan = Category
    End Sub

    Private CancelFlag As Boolean = False

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Protected Sub OnPropertyChanged(ByVal PropertyName As String)
        ' Raise the event, and make this procedure
        ' overridable, should someone want to inherit from
        ' this class and override this behavior:
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(PropertyName))
    End Sub

    Private Async Function ScanCategory(category As RecipeFolder) As Task
        CurrentCategory = category
        ProgressDeterminate.Minimum = 0
        ProgressDeterminate.Maximum = category.Recipes.Count
        ScannedCategory.Text = category.DisplayName
        Dim index As Integer = 0
        For Each r In category.Recipes
            If CancelFlag Then
                Return
            End If
            If r.File Is Nothing Then
                Continue For
            End If
            ProgressDeterminate.Value = index
            If r.Calories = Recipe.CaloriesNotScanned Then
                r.Calories = Await PDFSupport.PDFScanner.ScanPDFForCaloriesAsync(r.File)
                MetaDataDatabase.Current.StoreCalories(r.Category, r.Name, r.Calories)
            End If
            index = index + 1
        Next
    End Function

    Private Sub ScanForCaloriesProgressContentDialog_PrimaryButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)
        CancelFlag = True
    End Sub

    Private Async Sub ScanForCaloriesProgressContentDialog_Opened(sender As ContentDialog, args As ContentDialogOpenedEventArgs) Handles ScanForCaloriesProgressContentDialog.Opened

        If CategoryToScan Is Nothing Then
            For Each folder In RecipeFolders.Current.Folders
                If folder.IsSpecialFolder() Then
                    Continue For
                End If
                If CancelFlag Then
                    Hide()
                    Return
                End If
                If Not folder.ContentLoaded Then
                    Await folder.LoadAsync()
                End If
                If CancelFlag Then
                    Hide()
                    Return
                End If
                Await ScanCategory(folder)
            Next
            For Each folder In RecipeFolders.Current.SubFolders
                If CancelFlag Then
                    Hide()
                    Return
                End If
                If Not folder.ContentLoaded Then
                    Await folder.LoadAsync()
                End If
                If CancelFlag Then
                    Hide()
                    Return
                End If
                Await ScanCategory(folder)
            Next
        Else
            Await ScanCategory(CategoryToScan)
        End If
        Hide()

    End Sub

End Class
