Imports Windows.Storage
Imports Windows.Storage.Provider
Imports Windows.Globalization.DateTimeFormatting
Imports System.Text
Imports Windows.Storage.Streams
Imports RecipeBrowser.ViewModels
Imports RecipeBrowser.Common

Public Class Recipe
    Implements INotifyPropertyChanged

#Region "Properties"
    Public Event PropertyChanged(ByVal sender As Object, ByVal e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

    Protected Overridable Sub OnPropertyChanged(ByVal PropertyName As String)
        ' Raise the event, and make this procedure
        ' overridable, should someone want to inherit from
        ' this class and override this behavior:
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(PropertyName))
    End Sub

    Public Property RemoveFromFavoritesCommand As RelayCommand
    Public Property AddToFavoritesCommand As RelayCommand

    Private _DisplayCategory As String
    Public ReadOnly Property DisplayCategory As String ' Category without leading path
        Get
            Return _DisplayCategory
        End Get
    End Property

    Private _Category As String
    Public Property Category As String
        Get
            Return _Category
        End Get
        Set(value As String)
            If Not value.Equals(_Category) Then
                _Category = value
                _Path = _Category + "\" + _Name
                OnPropertyChanged("Path")
                Dim pos = value.LastIndexOf("\")
                If pos >= 0 Then
                    _DisplayCategory = value.Substring(pos + 1)
                Else
                    _DisplayCategory = value
                End If
            End If
        End Set
    End Property

    Public Property Parent As RecipeFolder

    Private _Name As String
    Public Property Name As String
        Get
            Return _Name
        End Get
        Set(value As String)
            If value <> _Name Then
                _Name = value
                _Path = _Category + "\" + _Name
                OnPropertyChanged("Name")
                OnPropertyChanged("Path")
            End If
        End Set
    End Property

    Private _SubTitle As String
    Public Property SubTitle As String
        Get
            Return _SubTitle
        End Get
        Set(value As String)
            If value <> _SubTitle Then
                _SubTitle = value
                OnPropertyChanged("SubTitle")
            End If
        End Set
    End Property

    Private _Path As String
    Public ReadOnly Property Path
        Get
            Return _Path
        End Get
    End Property

    Private _FavoritesSubTitle As String
    Public Property FavoritesSubTitle As String
        Get
            Return _FavoritesSubTitle
        End Get
        Set(value As String)
            If value <> _FavoritesSubTitle Then
                _FavoritesSubTitle = value
                OnPropertyChanged("FavoritesSubTitle")
            End If
        End Set
    End Property

    Public Property FolderIconVisibility As Visibility = Visibility.Collapsed

    Public Const CaloriesNotScanned As Integer = -1
    Public Const CaloriesUnknown As Integer = 0

    Private _calories As Integer = CaloriesNotScanned
    Public Property Calories As Integer
        Get
            Return _calories
        End Get
        Set(value As Integer)
            If value <> Calories Then
                _calories = value
                OnPropertyChanged("Calories")
            End If
        End Set
    End Property


    Private _isFavorite As Boolean = False
    Public Property IsFavorite As Boolean
        Get
            Return _isFavorite
        End Get
        Set(value As Boolean)
            If (value <> _isFavorite) Then
                _isFavorite = value
                OnPropertyChanged("IsFavorite")
            End If
        End Set
    End Property

    Private _pointerEntered As Boolean = False
    Public Property PointerEntered As Boolean
        Get
            Return _pointerEntered
        End Get
        Set(value As Boolean)
            If value <> _pointerEntered Then
                _pointerEntered = value
                OnPropertyChanged("PointerEntered")
            End If
        End Set
    End Property
    Public Property Tags As New ObservableCollection(Of RecipeTagViewModel)

    Public Property CreationDateTime As DateTime = DateTime.MinValue
    Public Property RenderSubTitleTask As Task
    Public Property AddedToFavoritesDateTime As DateTime
    Public Property NoOfPages As Integer
    Public Property CurrentPage As Integer
    Public Property RenderedPageNumber As Integer
    Public Property LastCooked As String
    Public Property CookedNoOfTimes As Integer
    Public Property ExternalSource As String
    Public Property InternetLink As String
    Public ReadOnly Property ExternalSourceVisible As Visibility
        Get
            If ExternalSource IsNot Nothing AndAlso ExternalSource.Length > 0 Then
                Return Visibility.Visible
            Else
                Return Visibility.Collapsed
            End If
        End Get
    End Property

    Public Enum ItemTypes
        Recipe
        ExternalRecipe
        SubCategory
        Header ' Used for "More.." line in history view
    End Enum
    Public Property ItemType As ItemTypes = ItemTypes.Recipe
    Public Property RecipeContentVisibility As Visibility = Visibility.Visible
    Public Property HeaderContentVisibility As Visibility = Visibility.Collapsed

    Public Property File As Windows.Storage.StorageFile
    Public ReadOnly Property RenderedPage As BitmapImage
        Get
            Return _RenderedPage
        End Get
    End Property

    Public Property Notes As Windows.Storage.StorageFile
    Public Property RecipeSource As Windows.Storage.StorageFile

    Public Property Pictures As ObservableCollection(Of RecipeImage)

    Public Overrides Function ToString() As String
        Return Name + " (" + Category + ")"
    End Function
#End Region

    Public Sub New()
        RemoveFromFavoritesCommand = New RelayCommand(AddressOf OnRemoveFromFavoritesCommand)
        AddToFavoritesCommand = New RelayCommand(AddressOf OnAddToFavoritesCommand)
    End Sub

#Region "Page Rendering"
    Private _RenderedPage As BitmapImage
    Private _RenderedPages As New List(Of BitmapImage)
    Private _Document As Windows.Data.Pdf.PdfDocument

    Private _PageRendererRunning As Boolean

    Private Async Function GetCreationDateTime() As Task

        If File IsNot Nothing Then
            Dim properties = Await File.GetBasicPropertiesAsync()
            CreationDateTime = properties.ItemDate.DateTime
        End If

    End Function

    Private Async Function _RenderSubTitleAsync() As Task

        If CreationDateTime = DateTime.MinValue AndAlso File IsNot Nothing Then
            Await GetCreationDateTime()
        End If
        RenderSubTitle()

    End Function

    Public Function RenderSubTitleAsync() As Task

        Return _RenderSubTitleAsync()

    End Function

    Public Sub RenderSubTitle()

        If CreationDateTime.Equals(Date.MinValue) Then
            SubTitle = DisplayCategory
        Else
            SubTitle = DisplayCategory + ", " + DateTimeFormatter.ShortDate.Format(CreationDateTime)
        End If
        If CookedNoOfTimes > 0 Then
            SubTitle = SubTitle + ", " + App.Texts.GetString("CookedOn") + " " + LastCooked
            If CookedNoOfTimes = 1 Then
                SubTitle = SubTitle + " (" + App.Texts.GetString("UpToNow") + " " + CookedNoOfTimes.ToString + " " + App.Texts.GetString("Time") + ")"
            Else
                SubTitle = SubTitle + " (" + App.Texts.GetString("UpToNow") + " " + CookedNoOfTimes.ToString + " " + App.Texts.GetString("Times") + ")"
            End If
        End If

        If AddedToFavoritesDateTime.Equals(Date.MinValue) Then
            FavoritesSubTitle = DisplayCategory
        Else
            FavoritesSubTitle = DisplayCategory + ", " + DateTimeFormatter.ShortDate.Format(AddedToFavoritesDateTime)
        End If
        If CookedNoOfTimes > 0 Then
            FavoritesSubTitle = FavoritesSubTitle + ", " + App.Texts.GetString("CookedOn") + " " + LastCooked
            If CookedNoOfTimes = 1 Then
                FavoritesSubTitle = FavoritesSubTitle + " (" + App.Texts.GetString("UpToNow") + " " + CookedNoOfTimes.ToString + " " + App.Texts.GetString("Time") + ")"
            Else
                FavoritesSubTitle = FavoritesSubTitle + " (" + App.Texts.GetString("UpToNow") + " " + CookedNoOfTimes.ToString + " " + App.Texts.GetString("Times") + ")"
            End If
        End If

    End Sub

    Public Async Function RenderPageAsyncOld2() As Task

        'If App.Logger.IsEnabled() Then
        '    Return
        '    'DEBUGCODE
        'End If

        If _Document Is Nothing Or RenderedPageNumber = CurrentPage - 1 Or _PageRendererRunning Then
            Return
        End If

        RenderedPageNumber = CurrentPage - 1

        If _RenderedPages.Count >= CurrentPage Then
            _RenderedPage = _RenderedPages.Item(RenderedPageNumber)
            Return
        End If

        _PageRendererRunning = True

        Dim errorOccured As Boolean = False
        Dim permissionDenied As Boolean = False

        Try
            Dim filename = Guid.NewGuid().ToString() + ".png"

            Dim page As Windows.Data.Pdf.PdfPage = _Document.GetPage(RenderedPageNumber)
            Await page.PreparePageAsync()
            Dim tempFolder = Windows.Storage.ApplicationData.Current.LocalFolder
            Dim tempFile As Windows.Storage.StorageFile = Await tempFolder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.ReplaceExisting)
            Using tempStream As Streams.IRandomAccessStream = Await tempFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
                App.Logger.Write("Rendering:" + filename)
                Try
                    Await page.RenderToStreamAsync(tempStream)
                Catch ex As Exception
                    App.Logger.WriteException("Render pdf failed", ex)
                End Try
                Await tempStream.FlushAsync()
            End Using
            page.Dispose()

            Dim renderedPicture = Await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(filename)

            If renderedPicture IsNot Nothing Then

                ' Open a stream for the selected file.
                Dim fileStream As Streams.IRandomAccessStream = Await renderedPicture.OpenAsync(Windows.Storage.FileAccessMode.Read)
                ' Set the image source to the selected bitmap.
                _RenderedPage = New Windows.UI.Xaml.Media.Imaging.BitmapImage()
                Await RenderedPage.SetSourceAsync(fileStream)
                fileStream.Dispose()
                _RenderedPages.Add(_RenderedPage)
            End If
        Catch ex1 As System.UnauthorizedAccessException
            permissionDenied = True
            App.Logger.WriteException("Access image failed (not authorized)", ex1)
            Exit Try
        Catch ex As Exception
            App.Logger.WriteException("Access image failed", ex)
            errorOccured = True
            Exit Try
        End Try

        _PageRendererRunning = False

        'If errorOccured Then
        '    Dim popup = New Windows.UI.Popups.MessageDialog("Das Rezept konnte nicht geladen werden.")
        '    Await popup.ShowAsync()
        '    Return Nothing
        'ElseIf permissionDenied Then
        '    Dim popup = New Windows.UI.Popups.MessageDialog("Der Zugriff auf das Rezept wurde verweigert.")
        '    Await popup.ShowAsync()
        '    Return Nothing
        'End If

    End Function

    Public Async Function RenderPageAsync() As Task

        If _Document Is Nothing Or RenderedPageNumber = CurrentPage - 1 Or _PageRendererRunning Then
            Return
        End If

        RenderedPageNumber = CurrentPage - 1

        If _RenderedPages.Count >= CurrentPage Then
            _RenderedPage = _RenderedPages.Item(RenderedPageNumber)
            Return
        End If

        _PageRendererRunning = True

        Dim errorOccured As Boolean = False
        Dim permissionDenied As Boolean = False

        Try
            Dim page As Windows.Data.Pdf.PdfPage = _Document.GetPage(RenderedPageNumber)
            Dim memoryStream = New InMemoryRandomAccessStream()
            Await page.PreparePageAsync()
            App.Logger.Write("Rendering:" + Name + " Page " + RenderedPageNumber.ToString)
            Try
                Await page.RenderToStreamAsync(memoryStream)
            Catch ex As Exception
                App.Logger.WriteException("Render pdf failed", ex)
            End Try
            page.Dispose()

            ' Set the image source to the selected bitmap.
            _RenderedPage = New Windows.UI.Xaml.Media.Imaging.BitmapImage()
            Await RenderedPage.SetSourceAsync(memoryStream)
            'memoryStream.Dispose()
            _RenderedPages.Add(_RenderedPage)
        Catch ex1 As System.UnauthorizedAccessException
            permissionDenied = True
            App.Logger.WriteException("Access image failed (not authorized)", ex1)
            Exit Try
        Catch ex As Exception
            App.Logger.WriteException("Access image failed", ex)
            errorOccured = True
            Exit Try
        End Try

        _PageRendererRunning = False

        'If errorOccured Then
        '    Dim popup = New Windows.UI.Popups.MessageDialog("Das Rezept konnte nicht geladen werden.")
        '    Await popup.ShowAsync()
        '    Return Nothing
        'ElseIf permissionDenied Then
        '    Dim popup = New Windows.UI.Popups.MessageDialog("Der Zugriff auf das Rezept wurde verweigert.")
        '    Await popup.ShowAsync()
        '    Return Nothing
        'End If

    End Function

    Public Async Function PreviousPage() As Task

        If CurrentPage > 1 Then
            CurrentPage = CurrentPage - 1
            Await RenderPageAsync()
        End If

    End Function

    Public Async Function NextPage() As Task

        If CurrentPage < NoOfPages Then
            CurrentPage = CurrentPage + 1
            Await RenderPageAsync()
        End If

    End Function

#End Region

#Region "Key conversion"
    Private Shared SeparatorString As String = "§§§§§§§"

    Public Function GetKey(folder As String) As String

        Return folder + SeparatorString + Category + SeparatorString + Name

    End Function

    Public Shared Sub GetCategoryAndNameFromKey(ByRef key As String, ByRef folder As String, ByRef category As String, ByRef name As String)

        Dim pos As Integer
        Dim tail As String

        folder = ""
        category = ""
        name = ""

        pos = key.IndexOf(SeparatorString)

        If pos = -1 Then
            Return
        End If

        folder = key.Substring(0, pos)
        tail = key.Substring(pos + SeparatorString.Count)

        pos = tail.IndexOf(SeparatorString)

        If pos = -1 Then
            Return
        End If

        category = tail.Substring(0, pos)
        name = tail.Substring(pos + SeparatorString.Count)

    End Sub

#End Region

#Region "Notes"
    Private Async Function WriteNotesToFileAsync(ByVal noteText As Windows.UI.Text.ITextDocument, ByVal file As Windows.Storage.StorageFile) As Task
        Try

            ' Prevent updates to the remote version of the file until we 
            ' finish making changes and call CompleteUpdatesAsync.
            CachedFileManager.DeferUpdates(file)
            ' write to file
            Dim randAccStream As Windows.Storage.Streams.IRandomAccessStream = Await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)

            noteText.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream)

            randAccStream.Dispose()

            ' Let Windows know that we're finished changing the file so the 
            ' other app can update the remote version of the file.
            Dim status As FileUpdateStatus = Await CachedFileManager.CompleteUpdatesAsync(file)
            If (status <> FileUpdateStatus.Complete) Then
                Dim errorBox As Windows.UI.Popups.MessageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("UnableToSaveNotes"))
                Await errorBox.ShowAsync()
            End If
        Catch ex As Exception
        End Try

    End Function

    Public Async Function UpdateNoteTextAsync(noteText As Windows.UI.Text.ITextDocument) As Task

        Dim allFolders = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        If Notes Is Nothing Then
            Dim recipeFolder = allFolders.GetFolder(Category)
            Try
                Notes = Await recipeFolder.Folder.CreateFileAsync(Name + ".rtf")
            Catch ex As Exception
            End Try
        End If

        If Notes IsNot Nothing Then
            Await WriteNotesToFileAsync(noteText, Notes)
            allFolders.UpdateNote(Me)
        End If

    End Function

    Public Async Function StoreInternetLinkAsync() As Task
        Await RecipeFolders.Current.UpdateMetadataAsync(Me)
    End Function

#End Region

#Region "LogAsCooked"
    Public Async Function LogRecipeCookedAsync(CookedOn As DateTimeOffset) As Task

        Dim allFolders = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        If Not History.Current.RecipeWasCookedOn(Category, Name, ConvertToDate(DateTimeFormatter.ShortDate.Format(CookedOn))) Then
            LastCooked = DateTimeFormatter.ShortDate.Format(CookedOn)
            CookedNoOfTimes = CookedNoOfTimes + 1

            Await allFolders.UpdateMetadataAsync(Me)
            History.Current.AddRecipe(Me)
        End If

    End Function

    Function CookedToday() As Boolean

        Return CookedOn(Date.Now)

    End Function

    Function CookedOn(OnDate As DateTimeOffset) As Boolean

        Return LastCooked IsNot Nothing AndAlso LastCooked.Equals(DateTimeFormatter.ShortDate.Format(OnDate))

    End Function


#End Region

#Region "Favorites"

    Private Async Sub OnRemoveFromFavoritesCommand()
        Await RecipesPage.Current.RemoveRecipeFromFavorites(Me)
    End Sub

    Private Async Sub OnAddToFavoritesCommand()
        Await RecipesPage.Current.AddRecipeToFavorites(Me)
    End Sub
#End Region

#Region "Load Recipe"

    Public Async Function LoadRecipeAsync() As Task

        If RenderedPage IsNot Nothing Then
            Return
        End If

        Try
            Dim tempStream As Streams.IRandomAccessStream = Await File.OpenAsync(Windows.Storage.FileAccessMode.Read)
            _Document = Await Windows.Data.Pdf.PdfDocument.LoadFromStreamAsync(tempStream)
            'tempStream.Dispose()
            NoOfPages = _Document.PageCount
            CurrentPage = 1
            RenderedPageNumber = -1 ' Force read
            Await RenderPageAsync()
            If Calories = Recipe.CaloriesNotScanned Then
                LoadCalories()
            End If
        Catch ex As Exception
            NoOfPages = 0
            CurrentPage = 0
        End Try

    End Function

    Private Async Sub LoadCalories()
        If Calories = CaloriesNotScanned Then
            Calories = MetaDataDatabase.Current.GetCalories(Category, Name)
            If Calories = CaloriesNotScanned Then
                Calories = Await PDFSupport.PDFScanner.ScanPDFForCaloriesAsync(_Document)
                MetaDataDatabase.Current.StoreCalories(Category, Name, Calories)
            End If
        End If
    End Sub

#End Region

#Region "Date conversion"
    Public Shared Function ConvertToDateStr(aDate As Date) As String
        Return DateTimeFormatter.ShortDate.Format(aDate)
    End Function

    Public Shared Function ConvertToDate(ByRef datestr As String) As DateTime

        Try
            ' Create two different encodings.
            Dim ascii As Encoding = Encoding.GetEncoding("US-ASCII")
            Dim unicode As Encoding = Encoding.Unicode

            ' Convert the string into a byte array.
            Dim unicodeBytes As Byte() = unicode.GetBytes(datestr)

            ' Perform the conversion from one encoding to the other.
            Dim asciiBytes As Byte() = Encoding.Convert(unicode, ascii, unicodeBytes)

            ' Convert the new byte array into a char array and then into a string.
            Dim asciiChars(ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length) - 1) As Char
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0)
            Dim asciiString As New String(asciiChars)

            Return DateTime.Parse(asciiString.Replace("?", ""))
        Catch ex As Exception
            Return Nothing
        End Try

    End Function

#End Region

#Region "Tags"

    Public Sub LoadTags()
        Tags.Clear()

        Dim directory = TagRepository.Current.Directory
        If directory Is Nothing OrElse directory.Count = 0 Then
            Return
        End If

        Dim allTags = MetaDataDatabase.Current.GetTags(Category, Name)

        If allTags IsNot Nothing AndAlso allTags.Count > 0 Then
            For Each t In allTags
                Tags.Add(New RecipeTagViewModel(Me, aTag:=t.Tag))
            Next
            If allTags.Count < directory.Count Then
                Tags.Add(New RecipeTagViewModel(Me, asPlus:=True))
            End If
        End If
    End Sub

    Public Function HasTag(aTag As String) As Boolean

        If Tags Is Nothing Then
            Return False
        End If
        Dim matches = Tags.Where(Function(otherTag) otherTag.Tag.Equals(aTag))
        Return matches.Count() > 0

    End Function

    Public Async Function AddTag() As Task
        Dim dialog As New ChooseTagsDialog()
        Do
            dialog.SetRecipe(Me)
            Await dialog.ShowAsync()
            If dialog.CreateTagRequested Then
                Dim createDialog As New DefineCategoryDialog(DefineCategoryDialog.ItemType.Tag)
                Await createDialog.ShowAsync()
            Else
                Exit Do
            End If
        Loop
    End Function

    Public Sub SetTags(tagList As List(Of RecipeTagViewModel))
        ' Invalidate tag folders
        For Each t In Tags
            If Not t.IsPlus AndAlso tagList.Where(Function(otherTag) otherTag.Tag.Equals(t.Tag)).Count() = 0 Then
                ' Tag removed
                Dim folder = RecipeFolders.Current.GetFolder(t.Tag)
                If folder.ContentLoaded Then
                    folder.DeleteRecipe(Me)
                End If
            End If
        Next

        For Each t In tagList
            If Not HasTag(t.Tag) Then
                ' Tag added
                Dim folder = RecipeFolders.Current.GetFolder(t.Tag)
                If folder.ContentLoaded Then
                    folder.Invalidate()
                End If
            End If
        Next

        'Rebuild tag list
        Tags.Clear()
        If tagList.Count > 0 Then
            For Each t In tagList
                Tags.Add(t)
            Next

            If Tags.Count < TagRepository.Current.Directory.Count Then
                Tags.Add(New RecipeTagViewModel(Me, asPlus:=True))
            End If
        End If
        SaveTags()
    End Sub

    Public Sub SaveTags()
        Dim allTags As New List(Of String)
        For Each t In Tags
            If Not t.IsPlus Then
                allTags.Add(t.Tag)
            End If
        Next
        MetaDataDatabase.Current.StoreTags(Category, Name, allTags)
    End Sub

    Public Sub DeleteTag(tag As RecipeTagViewModel)

        Dim folder = RecipeFolders.Current.GetFolder(tag.Tag)
        If folder.ContentLoaded Then
            folder.DeleteRecipe(Me)
        End If

        Tags.Remove(tag)

        ' Do not display "+" as only item
        If Tags.Count = 1 AndAlso Tags(0).IsPlus Then
            Tags.Clear() ' Remove isolated plus
        End If

        ' Make sure "+" is displayed if at least one tag is selected, but not all tags
        If Tags.Count > 0 AndAlso Tags.Count < TagRepository.Current.Directory.Count AndAlso Not Tags(Tags.Count - 1).IsPlus Then
            Tags.Add(New RecipeTagViewModel(Me, asPlus:=True))
        End If

        SaveTags()
    End Sub

#End Region

End Class

Public Class RecipeComparer_NameAscending
    Implements IComparer(Of Recipe)

    Public Function Compare(ByVal x As Recipe, ByVal y As Recipe) As Integer Implements IComparer(Of Recipe).Compare

        If x Is Nothing Then
            If y Is Nothing Then
                ' If x is Nothing and y is Nothing, they're
                ' equal. 
                Return 0
            Else
                ' If x is Nothing and y is not Nothing, y
                ' is greater. 
                Return -1
            End If
        Else
            ' If x is not Nothing...
            '
            If y Is Nothing Then
                ' ...and y is Nothing, x is greater.
                Return 1
            Else
                ' ...and y is not Nothing, compare the string
                Return x.Name.CompareTo(y.Name)
            End If
        End If
    End Function
End Class

Public Class RecipeComparer_DateDescending
    Implements IComparer(Of Recipe)

    Public Function Compare(ByVal x As Recipe, ByVal y As Recipe) As Integer Implements IComparer(Of Recipe).Compare

        If x Is Nothing Then
            If y Is Nothing Then
                ' If x is Nothing and y is Nothing, they're
                ' equal. 
                Return 0
            Else
                ' If x is Nothing and y is not Nothing, y
                ' is greater. 
                Return -1
            End If
        Else
            ' If x is not Nothing...
            '
            If y Is Nothing Then
                ' ...and y is Nothing, x is greater.
                Return 1
            Else
                ' ...and y is not Nothing, compare the string
                Return -1 * x.CreationDateTime.CompareTo(y.CreationDateTime)
            End If
        End If
    End Function
End Class

Public Class RecipeComparer_AddedToFavoritesDescending
    Implements IComparer(Of Recipe)

    Public Function Compare(ByVal x As Recipe, ByVal y As Recipe) As Integer Implements IComparer(Of Recipe).Compare

        If x Is Nothing Then
            If y Is Nothing Then
                ' If x is Nothing and y is Nothing, they're
                ' equal. 
                Return 0
            Else
                ' If x is Nothing and y is not Nothing, y
                ' is greater. 
                Return -1
            End If
        Else
            ' If x is not Nothing...
            '
            If y Is Nothing Then
                ' ...and y is Nothing, x is greater.
                Return 1
            Else
                ' ...and y is not Nothing, compare the string
                Return -1 * x.AddedToFavoritesDateTime.CompareTo(y.AddedToFavoritesDateTime)
            End If
        End If
    End Function
End Class

Public Class RecipeComparer_LastCookedDescending
    Implements IComparer(Of Recipe)

    Public Function Compare(ByVal x As Recipe, ByVal y As Recipe) As Integer Implements IComparer(Of Recipe).Compare

        If x Is Nothing OrElse x.LastCooked Is Nothing Then
            If y Is Nothing OrElse y.LastCooked Is Nothing Then
                ' If x is Nothing and y is Nothing, they're
                ' equal. 
                Return 0
            Else
                ' If x is Nothing and y is not Nothing, y
                ' is smaller. 
                Return 1
            End If
        Else
            ' If x is not Nothing...
            '
            If y Is Nothing OrElse y.LastCooked Is Nothing Then
                ' ...and y is Nothing, x is smaller.
                Return -1
            Else
                ' ...and y is not Nothing, compare the dates
                Dim xDate As DateTime
                Dim yDate As DateTime

                xDate = Recipe.ConvertToDate(x.LastCooked)
                yDate = Recipe.ConvertToDate(y.LastCooked)
                Return -1 * xDate.CompareTo(yDate)

            End If
        End If
    End Function
End Class
