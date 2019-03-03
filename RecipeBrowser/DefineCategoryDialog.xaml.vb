' Die Elementvorlage "Inhaltsdialog" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Imports Windows.UI
Imports RecipeBrowser.Controls
Imports RecipeBrowser.Persistency
Imports RecipeBrowser.ViewModels

Public NotInheritable Class DefineCategoryDialog
    Inherits ContentDialog
    Implements INotifyPropertyChanged

    Protected Sub OnPropertyChanged(ByVal PropertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(PropertyName))
    End Sub

    Private originalFolder As RecipeFolder
    Private creationMode As Boolean
    Private currentItemType As ItemType

    Private _backgroundColor As Color = Colors.White
    Public Property BackgroundColor As Color
        Get
            Return _backgroundColor
        End Get
        Set(value As Color)
            If Not value.Equals(_backgroundColor) Then
                _backgroundColor = value
                OnPropertyChanged("BackgroundColor")
            End If
        End Set
    End Property

    Private _foregroundColor As Color = Colors.Black
    Public Property ForegroundColor As Color
        Get
            Return _foregroundColor
        End Get
        Set(value As Color)
            If Not value.Equals(_foregroundColor) Then
                _foregroundColor = value
                OnPropertyChanged("ForegroundColor")
            End If
        End Set
    End Property

    Private _categoryName As String
    Public Property CategoryName As String
        Get
            Return _categoryName
        End Get
        Set(value As String)
            If Not value.Equals(_categoryName) Then
                _categoryName = value
                OnPropertyChanged("CategoryName")
            End If
        End Set
    End Property

    Event LoadImageRequested(ByVal imageFile As Windows.Storage.StorageFile)
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Property TagPalette As List(Of TagDirectoryViewModel)

    Public Enum ItemType
        Category
        Tag
    End Enum

    Public Sub New(type As ItemType, Optional originalName As String = Nothing, Optional Background As Color = Nothing, Optional Foreground As Color = Nothing)

        InitializeComponent()

        currentItemType = type

        If originalName Is Nothing Then
            originalFolder = Nothing
        Else
            Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
            originalFolder = categories.GetFolder(originalName)
        End If

        creationMode = (originalFolder Is Nothing)

        AddHandler LoadImageRequested, AddressOf OnLoadImageRequested

        If currentItemType = ItemType.Tag Then
            CategoryLabel.Text = App.Texts.GetString("TagLabel")
            'BackgroundColor = Background
            'ForegroundColor = Foreground
            If Background <> Nothing Then
                BackgroundColor = Background
            End If
            If Foreground <> Nothing Then
                ForegroundColor = Foreground
            End If
        Else
            ColorSettings.Visibility = Visibility.Collapsed
        End If

        If creationMode Then
            If currentItemType = ItemType.Tag Then
                CategoryEditor.Title = App.Texts.GetString("CreateTagTitle")
            End If
        Else
            CategoryName = originalFolder.Name
            TitleEditor.Text = originalFolder.Name
            If currentItemType = ItemType.Tag Then
                CategoryEditor.Title = App.Texts.GetString("EditTagTitle")
            Else
                CategoryEditor.Title = App.Texts.GetString("EditCategoryTitle")
            End If
            RaiseEvent LoadImageRequested(originalFolder.ImageFile)
        End If

        SetupTagPalette()
    End Sub

    Private Sub SetupTagPalette()
        If TagPalette Is Nothing Then
            TagPalette = New List(Of TagDirectoryViewModel)
            Dim label = App.Texts.GetString("TagLabel")
            For Each c In ColorUtilities.DesignColors
                TagPalette.Add(New TagDirectoryViewModel(label, c, ColorUtilities.GetForegroundColor(c)))
            Next
        End If
    End Sub

    Private SelectedImage As Windows.Storage.StorageFile

    Private Async Sub OnLoadImageRequested(ByVal imageFile As Windows.Storage.StorageFile)
        Await LoadImageAsync(imageFile)
    End Sub

    Private Async Function LoadImageAsync(ByVal imageFile As Windows.Storage.StorageFile) As Task

        If imageFile IsNot Nothing Then
            Try
                ' Open a stream for the selected file.
                Dim fileStream = Await imageFile.OpenAsync(Windows.Storage.FileAccessMode.Read)

                ' Set the image source to the selected bitmap.
                Dim BitmapImage = New Windows.UI.Xaml.Media.Imaging.BitmapImage()

                BitmapImage.SetSource(fileStream)
                CategoryImage.Source = BitmapImage
            Catch ex As Exception
            End Try
        End If

        SelectedImage = imageFile

    End Function

    Private Async Sub LoadCategoryImage_Click(sender As Object, e As RoutedEventArgs)

        Dim openPicker = New Windows.Storage.Pickers.FileOpenPicker()
        openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
        openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail

        ' Filter to include a sample subset of file types.
        openPicker.FileTypeFilter.Clear()
        openPicker.FileTypeFilter.Add(".png")
        openPicker.FileTypeFilter.Add(".jpg")

        ' Open the file picker.
        Dim file = Await openPicker.PickSingleFileAsync()

        ' file is null if user cancels the file picker.
        If file IsNot Nothing Then
            Await LoadImageAsync(file)

            If CategoryEditor.IsPrimaryButtonEnabled = False AndAlso CategoryNameIsValid() Then
                CategoryEditor.IsPrimaryButtonEnabled = True
            End If
        End If
    End Sub

    Class StringContainer
        Public content As New String("")
    End Class

    Private Function CategoryNameIsValid(Optional ByRef errorMessage As StringContainer = Nothing) As Boolean

        If TitleEditor.Text Is Nothing OrElse TitleEditor.Text.Trim().Equals("") Then
            If errorMessage IsNot Nothing Then
                errorMessage.content = App.Texts.GetString("CategoryNameIsEmpty")
            End If
            Return False
        End If

        If creationMode OrElse Not TitleEditor.Text.Trim().Equals(originalFolder.Name) Then
            Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

            If categories.GetFolder(TitleEditor.Text.Trim()) IsNot Nothing Then
                If errorMessage IsNot Nothing Then
                    errorMessage.content = App.Texts.GetString("CategoryDoesAlreadyExist")
                End If
                Return False
            End If
        End If

        If errorMessage IsNot Nothing Then
            errorMessage.content = ""
            CategoryName = TitleEditor.Text.Trim()
        End If
        Return True

    End Function

    Private Async Sub SaveButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)

        Dim errorMessage As New StringContainer()

        If CategoryNameIsValid(errorMessage) Then
            Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
            If creationMode Then
                If currentItemType = ItemType.Category Then
                    Await categories.CreateCategoryAsync(CategoryName.Trim(), SelectedImage)
                Else
                    Dim newTag = New TagDirectory(CategoryName.Trim(), BackgroundColor, ForegroundColor)
                    Await categories.CreateTagAsync(newTag, SelectedImage)
                End If
            Else
                If currentItemType = ItemType.Category Then
                    Await categories.ModifyCategoryAsync(originalFolder, CategoryName.Trim(), SelectedImage)
                Else
                    Dim newTag = New TagDirectory(CategoryName.Trim(), BackgroundColor, ForegroundColor)
                    Await categories.ModifyTagAsync(originalFolder, newTag, SelectedImage)
                End If
            End If
            CategoryEditor.Hide()
        Else
            Dim messageDialog = New Windows.UI.Popups.MessageDialog(errorMessage.content)
            Await messageDialog.ShowAsync()
            Return
        End If

    End Sub

    Private saveNecessary As Boolean

    Private Sub CategoryName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TitleEditor.TextChanged

        Dim errorMessage As New StringContainer()

        If CategoryNameIsValid(errorMessage) Then
            TitleEditor.SetValue(BorderBrushProperty, New SolidColorBrush(Windows.UI.Colors.Black))
            CategoryEditor.IsPrimaryButtonEnabled = True
        Else
            TitleEditor.SetValue(BorderBrushProperty, New SolidColorBrush(Windows.UI.Colors.Red))
            CategoryEditor.IsPrimaryButtonEnabled = False
        End If

        ErrorMessageDisplay.Text = errorMessage.content

        saveNecessary = True
    End Sub


    Private Sub CancelButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)

        CategoryEditor.Hide()

    End Sub

    Private Sub TagPalette_ItemClick(sender As Object, e As ItemClickEventArgs) Handles TagPaletteListView.ItemClick
        Dim selected = DirectCast(e.ClickedItem, TagDirectoryViewModel)
        BackgroundColor = selected.Background
        ForegroundColor = selected.Foreground
        DisplayTagPalette.Flyout.Hide()
    End Sub
End Class
