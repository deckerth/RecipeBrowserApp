' Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Imports Windows.ApplicationModel.DataTransfer
Imports Windows.Foundation.Metadata
Imports Windows.Globalization.DateTimeFormatting
Imports Windows.Storage
Imports Windows.UI.Core
Imports Windows.UI.Popups
Imports Windows.UI.Xaml.Media.Animation
''' <summary>
''' Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
''' </summary>
Public NotInheritable Class RecipePage
    Inherits Page

    Private CurrentRecipeFolder As RecipeFolder
    Public Property OtherCategoryList As New ObservableCollection(Of FolderDescriptor)

    Public Property TimerController As Timers.Controller

    Public Property BackButtonVisibility As Visibility

    Private _lastSelectedItem As Recipe
    ''' <summary>
    ''' NavigationHelper wird auf jeder Seite zur Unterstützung bei der Navigation verwendet und 
    ''' Verwaltung der Prozesslebensdauer
    ''' </summary>
    Public ReadOnly Property NavigationHelper As Common.NavigationHelper
        Get
            Return Me._navigationHelper
        End Get
    End Property
    Private _navigationHelper As Common.NavigationHelper


    Public Sub New()
        InitializeComponent()
        Me._navigationHelper = New Common.NavigationHelper(Me)
        AddHandler Me._navigationHelper.LoadState, AddressOf NavigationHelper_LoadState
        AddHandler Me._navigationHelper.SaveState, AddressOf NavigationHelper_SaveState

        TimerController = Timers.Controller.Current

        Dim manager = DataTransferManager.GetForCurrentView()
        AddHandler manager.DataRequested, AddressOf DataRequestedManager
    End Sub


    ''' <summary>
    ''' Füllt die Seite mit Inhalt auf, der bei der Navigation übergeben wird.  Gespeicherte Zustände werden ebenfalls
    ''' bereitgestellt, wenn eine Seite aus einer vorherigen Sitzung neu erstellt wird.
    ''' </summary>
    ''' 
    ''' Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/>
    ''' 
    ''' <param name="e">Ereignisdaten, die die Navigationsparameter bereitstellen, die an
    ''' <see cref="Frame.Navigate"/> übergeben wurde, als diese Seite ursprünglich angefordert wurde und
    ''' ein Wörterbuch des Zustands, der von dieser Seite während einer früheren
    ''' beibehalten wurde.  Der Zustand ist beim ersten Aufrufen einer Seite NULL.</param>
    ''' 
    Private Async Sub NavigationHelper_LoadState(sender As Object, e As Common.LoadStateEventArgs)

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        Dim key As String
        Dim folder As String
        Dim category As String
        Dim name As String

        key = DirectCast(e.NavigationParameter, String)
        Recipe.GetCategoryAndNameFromKey(key, folder, category, name)

        DisableControls()

        CurrentRecipeFolder = Await categories.GetFolderAsync(folder)

        AddToFavorites.Visibility = Visibility.Collapsed
        RemoveFromFavorites.Visibility = Visibility.Collapsed
        ShowFavoritesButton.Visibility = CurrentRecipeFolder.FavoritesVisibility
        EditTags.Visibility = CurrentRecipeFolder.EditTagsVisibility
        ShowHistoryButton.Visibility = CurrentRecipeFolder.HistoryVisibility
        ChangeCategory.Visibility = CurrentRecipeFolder.ChangeCategoryVisibility
        logAsCooked.Visibility = CurrentRecipeFolder.LogAsCookedVisibility
        ShowImageGalery.Visibility = CurrentRecipeFolder.ShowImageGalleryVisibility
        OpenFile.Visibility = CurrentRecipeFolder.OpenFileVisibility
        Share.Visibility = CurrentRecipeFolder.ShareVisibility
        deleteRecipe.Visibility = CurrentRecipeFolder.DeleteRecipeVisibility
        RenameRecipe.Visibility = CurrentRecipeFolder.DeleteRecipeVisibility
        ChangeCalories.Visibility = CurrentRecipeFolder.ChangeCaloriesVisibility
        AppHelpButton.Visibility = CurrentRecipeFolder.HelpVisibility
        LastAddedSearchButton.Visibility = CurrentRecipeFolder.LastAddedSearchVisibility

        If ApiInformation.IsPropertyPresent("Windows.UI.Xaml.FrameworkElement", "AllowFocusOnInteraction") Then
            editNote.AllowFocusOnInteraction = True ' Otherwise the note editor does not accept input
        End If

        If EditTags.Visibility = Visibility.Visible AndAlso TagRepository.Current.Directory.Count = 0 Then
            EditTags.Visibility = Visibility.Collapsed 'No tags defined
        End If

        EnableControls()

        _lastSelectedItem = Await categories.GetRecipeAsync(folder, category, name)

        If _lastSelectedItem.Calories > 0 Then
            pageTitle.Text = name + " (" + _lastSelectedItem.Calories.ToString + " kcal)"
        Else
            pageTitle.Text = name
        End If

        'Await Timers.Factory.Current.WakeUp()
        Await DisplayCurrentItemDetail()

    End Sub

    ''' <summary>
    ''' Behält den dieser Seite zugeordneten Zustand bei, wenn die Anwendung angehalten oder
    ''' die Seite im Navigationscache verworfen wird.  Die Werte müssen den Serialisierungsanforderungen
    ''' von <see cref="Common.SuspensionManager.SessionState"/> entsprechen.
    ''' </summary>
    ''' <param name="sender">
    ''' Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/>
    ''' </param>
    ''' <param name="e">Ereignisdaten, die ein leeres Wörterbuch zum Auffüllen bereitstellen 
    ''' serialisierbarer Zustand.</param>
    Private Sub NavigationHelper_SaveState(sender As Object, e As Common.SaveStateEventArgs)
        ' TODO: Einen serialisierbaren Navigationsparameter ableiten und ihn
        '       pageState("SelectedItem")
    End Sub

#Region "NavigationHelper-Registrierung"

    ''' Die in diesem Abschnitt bereitgestellten Methoden werden einfach verwendet, um
    ''' damit NavigationHelper auf die Navigationsmethoden der Seite reagieren kann.
    ''' 
    ''' Platzieren Sie seitenspezifische Logik in Ereignishandlern für  
    ''' <see cref="Common.NavigationHelper.LoadState"/>
    ''' and <see cref="Common.NavigationHelper.SaveState"/>.
    ''' Der Navigationsparameter ist in der LoadState-Methode verfügbar 
    ''' zusätzlich zum Seitenzustand, der während einer früheren Sitzung beibehalten wurde.

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        Dim currentView = SystemNavigationManager.GetForCurrentView()

        If ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons") OrElse
           Common.DeviceTypeHelper.GetDeviceFormFactorType() = Common.DeviceTypeHelper.DeviceFormFactorType.Tablet Then
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
            BackButtonVisibility = Visibility.Visible
        Else
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
            BackButtonVisibility = Visibility.Collapsed
        End If

        'currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
        'BackButtonVisibility = Visibility.Visible

        _navigationHelper.OnNavigatedTo(e)
    End Sub


    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        _navigationHelper.OnNavigatedFrom(e)
    End Sub

    Private Sub BackButton_Click(sender As Object, e As RoutedEventArgs) Handles BackButton.Click
        If Me.Frame IsNot Nothing AndAlso Me.Frame.CanGoBack Then
            Me.Frame.GoBack()
        End If
    End Sub

#End Region

#Region "MasterDetailHandling"
    Private Async Function DisplayCurrentItemDetail() As Task

        If _lastSelectedItem IsNot Nothing Then
            _lastSelectedItem.LoadTags()
            TagsList.ItemsSource = _lastSelectedItem.Tags
            If _lastSelectedItem.File IsNot Nothing Then
                ' Display PDF
                Await _lastSelectedItem.LoadRecipeAsync()
                itemDetail.Visibility = Visibility.Visible
                RecipeViewer.Visibility = Visibility.Visible
                RecipeSourceViewer.Visibility = Visibility.Visible
                RecipeViewer.Source = _lastSelectedItem.RenderedPage
            ElseIf _lastSelectedItem.RecipeSource IsNot Nothing Then
                ' Display RTF
                itemDetail.Visibility = Visibility.Visible
                RecipeViewer.Visibility = Visibility.Collapsed
                RecipeSourceViewer.Visibility = Visibility.Visible
                Try
                    Dim randAccStream As Windows.Storage.Streams.IRandomAccessStream =
                      Await _lastSelectedItem.RecipeSource.OpenAsync(Windows.Storage.FileAccessMode.Read)
                    RecipeSourceViewer.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, randAccStream)
                Catch ex As Exception
                End Try
            Else
                ' Display picture from galery
                Dim folder As RecipeFolder = Await RecipeFolders.Current.GetFolderAsync(_lastSelectedItem.Category)
                If folder IsNot Nothing Then
                    Await folder.GetImagesOfRecipeAsync(_lastSelectedItem)
                    If _lastSelectedItem.Pictures IsNot Nothing AndAlso _lastSelectedItem.Pictures.Count > 0 Then
                        RecipeViewer.Source = _lastSelectedItem.Pictures(0).Image
                    Else
                        RecipeViewer.Source = Nothing
                    End If
                Else
                    RecipeViewer.Source = Nothing
                End If
            End If
        Else
            RecipeViewer.Source = Nothing
        End If
        EnableControls()

    End Function
#End Region

#Region "PageControl"
    Private Sub RenderPageControl(ByRef CurrentRecipe As Recipe)

        If CurrentRecipe Is Nothing Then
            pageControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed
        Else
            If CurrentRecipe.NoOfPages > 1 Then
                pageNumber.Text = CurrentRecipe.CurrentPage.ToString + "/" + CurrentRecipe.NoOfPages.ToString
                pageControl.Visibility = Windows.UI.Xaml.Visibility.Visible
                If CurrentRecipe.CurrentPage = 1 Then
                    prevPage.IsEnabled = False
                Else
                    prevPage.IsEnabled = True

                End If
                If CurrentRecipe.CurrentPage = CurrentRecipe.NoOfPages Then
                    nextPage.IsEnabled = False
                Else
                    nextPage.IsEnabled = True
                End If
            Else
                pageControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed
            End If
        End If

    End Sub

    Private Async Sub GotoPreviousPage(sender As Object, e As RoutedEventArgs) Handles prevPage.Click

        If _lastSelectedItem Is Nothing Then
            Return
        End If
        DisableControls()
        Await _lastSelectedItem.PreviousPage()
        RecipeViewer.Source = _lastSelectedItem.RenderedPage
        EnableControls()

    End Sub

    Private Async Sub GotoNextPage(sender As Object, e As RoutedEventArgs) Handles nextPage.Click

        If _lastSelectedItem Is Nothing Then
            Return
        End If
        DisableControls()
        Await _lastSelectedItem.NextPage()
        RecipeViewer.Source = _lastSelectedItem.RenderedPage
        EnableControls()
    End Sub

#End Region

#Region "EnableDisableControls"
    Private Sub DisableControls(Optional visualizeProgress As Boolean = True)

        ShowFavorites.IsEnabled = False
        Home.IsEnabled = False
        ShowHistory.IsEnabled = False
        FolderSelection.IsEnabled = False
        ShowLastAdded.IsEnabled = False
        If visualizeProgress Then
            actionProgress.IsActive = True
        End If
        nextPage.IsEnabled = False
        prevPage.IsEnabled = False
        AddToFavorites.IsEnabled = False
        RemoveFromFavorites.IsEnabled = False
        OpenFile.IsEnabled = False
        ChangeCategory.IsEnabled = False
        deleteRecipe.IsEnabled = False
        ChangeCalories.IsEnabled = False
        RenameRecipe.IsEnabled = False
        editNote.IsEnabled = False
        EditFileButton.IsEnabled = False
        logAsCooked.IsEnabled = False
        Share.IsEnabled = False
        EditTags.IsEnabled = False

    End Sub

    Private Sub EnableControls()

        ShowFavorites.IsEnabled = True
        Home.IsEnabled = True
        actionProgress.IsActive = False
        ShowHistory.IsEnabled = True
        FolderSelection.IsEnabled = True
        EditTags.IsEnabled = True
        ShowLastAdded.IsEnabled = True

        RenderPageControl(_lastSelectedItem) ' currentRecipe may be nothing

        ' Functions for the current recipe
        If _lastSelectedItem Is Nothing Then
            ' Hide if nothing is selected
            AddToFavorites.IsEnabled = False
            RemoveFromFavorites.IsEnabled = False
            OpenFile.IsEnabled = False
            ChangeCategory.IsEnabled = False
            deleteRecipe.IsEnabled = False
            ChangeCalories.IsEnabled = False
            logAsCooked.IsEnabled = False
            EditTags.IsEnabled = False
            editNote.Label = ""
            editNote.SetValue(ForegroundProperty, New SolidColorBrush(Windows.UI.Colors.Gray))
            editNote.IsEnabled = False
            Share.IsEnabled = False
            RenameRecipe.IsEnabled = False
            EditFileButton.IsEnabled = False
        Else
            Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

            ShowImageGalery.IsEnabled = True
            RenameRecipe.IsEnabled = True
            ChangeCategory.IsEnabled = True
            ChangeCalories.IsEnabled = True

            If _lastSelectedItem.File IsNot Nothing OrElse _lastSelectedItem.RecipeSource IsNot Nothing Then
                ' Recipe content (pdf or rtf) is available
                OpenFile.IsEnabled = True
                Share.IsEnabled = True

                ' Recipes with pdf: Allow edit note, no direct editing
                If _lastSelectedItem.RecipeSource Is Nothing Then
                    editNote.Visibility = Visibility.Visible
                    EditFileButton.Visibility = Visibility.Collapsed
                    editNote.IsEnabled = True
                    If _lastSelectedItem.Notes Is Nothing AndAlso _lastSelectedItem.InternetLink Is Nothing Then
                        editNote.Label = App.Texts.GetString("CreateNote")
                        editNote.SetValue(ForegroundProperty, App.Current.Resources("MenuBarForegroundBrush"))
                    Else
                        editNote.Label = App.Texts.GetString("DisplayNote")
                        editNote.SetValue(ForegroundProperty, App.Current.Resources("NoteExistsColor"))
                    End If
                Else
                    editNote.Visibility = Visibility.Collapsed
                    EditFileButton.Visibility = Visibility.Visible
                    EditFileButton.IsEnabled = True
                End If
            Else
                editNote.IsEnabled = False
                EditFileButton.IsEnabled = False
                OpenFile.IsEnabled = False
                Share.IsEnabled = False
            End If

            'Favorites handling (not on templates page, toggle add and remove)
            AddToFavorites.IsEnabled = True
            RemoveFromFavorites.IsEnabled = True
            If CurrentRecipeFolder.Name = RecipeTemplates.FolderName Or
                   CurrentRecipeFolder.Name = HelpDocuments.FolderName Then
                RemoveFromFavorites.Visibility = Visibility.Collapsed
                AddToFavorites.Visibility = Visibility.Collapsed
            ElseIf CurrentRecipeFolder.Name = Favorites.FolderName OrElse categories.FavoriteFolder.IsFavorite(_lastSelectedItem) Then
                RemoveFromFavorites.Visibility = Windows.UI.Xaml.Visibility.Visible
                AddToFavorites.Visibility = Windows.UI.Xaml.Visibility.Collapsed
            Else
                RemoveFromFavorites.Visibility = Windows.UI.Xaml.Visibility.Collapsed
                AddToFavorites.Visibility = Windows.UI.Xaml.Visibility.Visible
            End If

            deleteRecipe.IsEnabled = _lastSelectedItem IsNot Nothing AndAlso _lastSelectedItem.ItemType = Recipe.ItemTypes.Recipe
            logAsCooked.IsEnabled = Not _lastSelectedItem.CookedToday()
        End If

    End Sub

#End Region

#Region "NoteEditor"
    Private noteTextChanged As Boolean
    Private recipeWithNote As Recipe

    Private Sub noteEditor_TextChanged(sender As Object, e As RoutedEventArgs) Handles noteEditor.TextChanged
        noteTextChanged = True
    End Sub

    Private Async Sub OpenNoteEditor_Click(sender As Object, e As RoutedEventArgs) Handles editNote.Click

        If _lastSelectedItem Is Nothing Then
            Return
        End If

        recipeWithNote = Nothing
        DisableControls(False) ' no progress display
        recipeWithNote = _lastSelectedItem
        noteEditor.Document.SetText(Windows.UI.Text.TextSetOptions.None, "")
        InternetLinkTextBox.Text = ""

        If recipeWithNote.Notes IsNot Nothing Then
            Try
                Dim randAccStream As Windows.Storage.Streams.IRandomAccessStream = Await recipeWithNote.Notes.OpenAsync(Windows.Storage.FileAccessMode.Read)
                noteEditor.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, randAccStream)
            Catch ex As Exception
            End Try
        End If

        If recipeWithNote.InternetLink IsNot Nothing Then
            InternetLinkTextBox.Text = recipeWithNote.InternetLink
        Else
            OpenInternetLink.IsEnabled = False
        End If

        noteTextChanged = False
    End Sub

    Private Async Sub NoteEditorFlyoutClosed(sender As Object, e As Object)

        actionProgress.IsActive = True
        Dim skipSave As Boolean = False
        If noteTextChanged Then
            If recipeWithNote.Notes Is Nothing Then
                Dim content As String = ""
                noteEditor.Document.GetText(Windows.UI.Text.TextGetOptions.None, content)
                skipSave = content.Length <= 1
            End If
            If Not skipSave Then
                Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
                Await recipeWithNote.UpdateNoteTextAsync(noteEditor.Document)
            End If
        End If

        If InternetLinkTextBox.Text.Length > 0 Or recipeWithNote.InternetLink IsNot Nothing Then
            recipeWithNote.InternetLink = InternetLinkTextBox.Text
            Await recipeWithNote.StoreInternetLinkAsync()
        End If

        EnableControls()

    End Sub

    Private Sub InternetLinkTextBox_TextChanged(sender As Object, e As TextChangedEventArgs) Handles InternetLinkTextBox.TextChanged

        OpenInternetLink.IsEnabled = (InternetLinkTextBox.Text.Length > 0)

    End Sub

    Private Async Function OpenInternetLink_ClickAsync(sender As Object, e As RoutedEventArgs) As Task Handles OpenInternetLink.Click

        Await Windows.System.Launcher.LaunchUriAsync(New Uri(InternetLinkTextBox.Text))

    End Function

#End Region

#Region "ContentSharing"

    Private Sub Share_Click(sender As Object, e As RoutedEventArgs) Handles Share.Click

        Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI()

    End Sub

    Private Sub DataRequestedManager(sender As DataTransferManager, args As DataRequestedEventArgs)

        ' Share a recipe

        If _lastSelectedItem Is Nothing Then
            Return
        End If

        Dim request = args.Request
        Dim storageItems As New List(Of IStorageItem)

        If _lastSelectedItem.File IsNot Nothing Then
            storageItems.Add(_lastSelectedItem.File)
        Else
            storageItems.Add(_lastSelectedItem.RecipeSource)
        End If

        request.Data.Properties.Title = App.Texts.GetString("Recipe")
        request.Data.Properties.Description = _lastSelectedItem.Name
        request.Data.SetStorageItems(storageItems)

    End Sub
#End Region

#Region "OpenAndFullscreen"

    Private Async Sub OpenFile_Click(sender As Object, e As RoutedEventArgs) Handles OpenFile.Click

        If _lastSelectedItem IsNot Nothing Then
            DisableControls()
            If _lastSelectedItem.File IsNot Nothing Then
                Await Windows.System.Launcher.LaunchFileAsync(_lastSelectedItem.File)
            Else
                Await Windows.System.Launcher.LaunchFileAsync(_lastSelectedItem.RecipeSource)
            End If
            EnableControls()
        End If

    End Sub

#End Region

#Region "Favorites"
    Private Async Sub AddToFavorites_Click(sender As Object, e As RoutedEventArgs) Handles AddToFavorites.Click

        If _lastSelectedItem IsNot Nothing Then
            DisableControls()

            Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
            Await categories.FavoriteFolder.AddRecipeAsync(_lastSelectedItem)
            EnableControls()
        End If

    End Sub

    Private Async Sub RemoveFromFavorites_Click(sender As Object, e As RoutedEventArgs) Handles RemoveFromFavorites.Click

        If _lastSelectedItem Is Nothing Then
            Return
        End If

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
        If Not categories.FavoriteFolder.ContentLoaded Then
            DisableControls(True)
            Await categories.FavoriteFolder.LoadAsync()
        End If
        categories.FavoriteFolder.DeleteRecipe(_lastSelectedItem)
        If CurrentRecipeFolder.Name = Favorites.FolderName Then
            RecipeViewer.Source = Nothing
            TagsList.ItemsSource = Nothing
            _lastSelectedItem = Nothing
            Me.Frame.GoBack()
        End If
        EnableControls()

    End Sub
#End Region

#Region "LastAdded"
    Private flyout As DatePickerFlyout

    Private Sub ShowAddedSince(aDate As DateTime)
        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
        categories.LastAddedFolder.SetSearchParameter(aDate)
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), LastAddedFolder.FolderName)
    End Sub

    Private Sub AddedSinceLastMonth_Click(sender As Object, e As RoutedEventArgs)
        ShowAddedSince(DateTime.Now.AddMonths(-1))
    End Sub

    Private Sub AddedSinceLast3Month_Click(sender As Object, e As RoutedEventArgs)
        ShowAddedSince(DateTime.Now.AddMonths(-3))
    End Sub

    Private Sub AddedSinceDate_Click(sender As Object, e As RoutedEventArgs)
        flyout = New DatePickerFlyout()
        AddHandler flyout.DatePicked, AddressOf AddedSinceDate_Picked
        flyout.ShowAt(ShowLastAdded)
    End Sub

    Private Sub AddedSinceDate_Picked(sender As DatePickerFlyout, args As DatePickedEventArgs)
        ShowAddedSince(flyout.Date.DateTime)
    End Sub

#End Region

#Region "Navigation"
    Private Sub ShowFavorites_Click(sender As Object, e As RoutedEventArgs) Handles ShowFavorites.Click
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), Favorites.FolderName)
    End Sub

    Private Sub FavoriteRecipesText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles FavoriteRecipesText.Tapped
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), Favorites.FolderName)
    End Sub

    Private Sub ToggleSplitView_Click(sender As Object, e As RoutedEventArgs) Handles ToggleSplitView.Click
        RootSplitView.IsPaneOpen = Not RootSplitView.IsPaneOpen
    End Sub

    Private Sub Home_Click(sender As Object, e As RoutedEventArgs) Handles Home.Click
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(CategoryOverview))
    End Sub

    Private Sub HomeText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles HomeText.Tapped
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(CategoryOverview))
    End Sub

    Private Sub CommandBar_Opened(sender As Object, e As Object)
        Dim cb = DirectCast(sender, CommandBar)
        If cb IsNot Nothing Then
            cb.Background.Opacity = 1.0
        End If
    End Sub

#End Region

#Region "LogAsCooked"
    Private Sub LogAsCooked_Click(sender As Object, e As RoutedEventArgs) Handles logAsCooked.Click

        If _lastSelectedItem Is Nothing Then
            Return
        End If

        DisableControls(False)
        CookedOn.Date = Date.Now

    End Sub


    Private Sub CookedOn_Closed(sender As Object, e As Object) Handles CookedOn.Closed
        EnableControls()
    End Sub

    Private Async Sub CookedOn_DatePicked(sender As DatePickerFlyout, args As DatePickedEventArgs) Handles CookedOn.DatePicked
        DisableControls()

        If _lastSelectedItem Is Nothing Then
            Return
        End If

        Dim convertedDate As Date = Recipe.ConvertToDate(DateTimeFormatter.ShortDate.Format(CookedOn.Date))

        If History.Current.RecipeWasCookedOn(_lastSelectedItem.Category, _lastSelectedItem.Name, convertedDate) Then
            Dim msg = New MessageDialog(App.Texts.GetString("LastCookedDateAlreadyStored"))
            Await msg.ShowAsync()
            Return
        End If

        Await _lastSelectedItem.LogRecipeCookedAsync(CookedOn.Date)

        EnableControls()
    End Sub

#End Region

#Region "DeleteRecipe"
    Private Cancelled As Boolean

    Private Async Sub DeleteRecipe_Click(sender As Object, e As RoutedEventArgs) Handles deleteRecipe.Click

        If _lastSelectedItem Is Nothing Then
            Return
        End If

        Dim messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("DoYouWantToDelete"))

        ' Add buttons and set their callbacks
        messageDialog.Commands.Add(New UICommand(App.Texts.GetString("Yes"), Sub(command)
                                                                                 Cancelled = False
                                                                             End Sub))

        messageDialog.Commands.Add(New UICommand(App.Texts.GetString("No"), Sub(command)
                                                                                Cancelled = True
                                                                            End Sub))

        ' Set the command that will be invoked by default
        messageDialog.DefaultCommandIndex = 1

        ' Set the command to be invoked when escape is pressed
        messageDialog.CancelCommandIndex = 1

        Await messageDialog.ShowAsync()

        If Cancelled Then
            Return
        End If

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        RecipeViewer.Source = Nothing
        DisableControls()
        Await categories.DeleteRecipeAsync(_lastSelectedItem)
        _lastSelectedItem = Nothing
        TagsList.ItemsSource = Nothing
        EnableControls()

        NavigationHelper.GoBack()
    End Sub
#End Region

#Region "Category Chooser"
    Private Enum ChooserModes
        ForChangeCategory
        ForFolderSelection
        ForCriteriaSelection
    End Enum

    Private ChooserMode As ChooserModes

    Public Sub ShowCategoryChooser()
        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        DisableControls(False)

        OtherCategoryList.Clear()
        If ChooserMode = ChooserModes.ForCriteriaSelection Then
            ' Build a list of criteria. Mark criteria of current recipe
            OtherCategoryList.Clear()
            For Each T In TagRepository.Current.Directory
                Dim folder As New FolderDescriptor With {.CategoryWithIndent = T.Tag, .CategoryPath = T.Tag}
                If _lastSelectedItem.HasTag(T.Tag) Then
                    folder.IndicatorColor = DirectCast(App.Current.Resources("CategoryIndicatorBrush"), SolidColorBrush)
                    folder.FontColor = folder.IndicatorColor
                Else
                    folder.IndicatorColor = New SolidColorBrush(Windows.UI.Colors.Transparent)
                    folder.FontColor = App.Current.Resources("MenuBarForegroundBrush")
                End If
                OtherCategoryList.Add(folder)
            Next
        Else
            ' Build a list of categories. The current category is marked.
            OtherCategoryList.Clear()
            For Each folder In categories.CategoryHierarchy
                If folder.CategoryPath = _lastSelectedItem.Category Then
                    folder.IndicatorColor = DirectCast(App.Current.Resources("CategoryIndicatorBrush"), SolidColorBrush)
                    folder.FontColor = folder.IndicatorColor
                Else
                    folder.IndicatorColor = New SolidColorBrush(Windows.UI.Colors.Transparent)
                    folder.FontColor = App.Current.Resources("MenuBarForegroundBrush")
                End If
                OtherCategoryList.Add(folder)
            Next
        End If

        FolderSelectionList.ItemsSource = OtherCategoryList

        Select Case ChooserMode
            Case ChooserModes.ForFolderSelection, ChooserModes.ForCriteriaSelection
                CategoryChooserTitle.Visibility = Visibility.Collapsed
            Case ChooserModes.ForChangeCategory
                CategoryChooserTitle.Visibility = Visibility.Visible
                CategoryChooserTitle.Text = App.Texts.GetString("MoveTo.Text")
        End Select

        RootSplitView.IsPaneOpen = False
        FolderSelectionSplitView.IsPaneOpen = True
    End Sub

    Private Async Sub Category_Chosen(sender As Object, e As ItemClickEventArgs)
        EnableControls()

        Dim selectedItem = DirectCast(e.ClickedItem, FolderDescriptor)

        If selectedItem IsNot Nothing Then
            FolderSelectionSplitView.IsPaneOpen = False
            If ChooserMode = ChooserModes.ForFolderSelection OrElse ChooserMode = ChooserModes.ForCriteriaSelection Then
                FolderSelectionChosen(selectedItem)
            Else
                Await ChangeCategoryOfCurrentItem(selectedItem)
            End If
        End If
    End Sub

    Private Sub FolderSelectionSplitView_PaneClosed(sender As SplitView, args As Object) Handles FolderSelectionSplitView.PaneClosed
        EnableControls()
    End Sub

#End Region

#Region "ChangeCategory"
    Private Sub changeCategory_Click(sender As Object, e As RoutedEventArgs) Handles ChangeCategory.Click
        ChooserMode = ChooserModes.ForChangeCategory
        ShowCategoryChooser()
    End Sub


    Private Async Function ChangeCategoryOfCurrentItem(newCategory As FolderDescriptor) As Task

        If _lastSelectedItem Is Nothing OrElse newCategory.CategoryPath = _lastSelectedItem.Category Then
            Return
        End If

        DisableControls()

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
        Dim newFolder As RecipeFolder = categories.GetFolder(newCategory.CategoryPath)
        If newFolder IsNot Nothing Then
            Await categories.ChangeCategoryAsync(_lastSelectedItem, newFolder)
        End If

        Await DisplayCurrentItemDetail()

        EnableControls()

    End Function
#End Region

#Region "FolderSelection"
    Private Sub FolderSelection_Click(sender As Object, e As RoutedEventArgs) Handles FolderSelection.Click
        ChooserMode = ChooserModes.ForFolderSelection
        ShowCategoryChooser()
    End Sub

    Private Sub FolderSelectionChosen(chosenCategory As FolderDescriptor)
        Me.Frame.Navigate(GetType(RecipesPage), chosenCategory.CategoryPath)
    End Sub

    Private Sub FolderSelectionText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles FolderSelectionText.Tapped
        ChooserMode = ChooserModes.ForFolderSelection
        ShowCategoryChooser()
    End Sub

    Private Sub CriteriaSelection_Click(sender As Object, e As RoutedEventArgs)
        ChooserMode = ChooserModes.ForCriteriaSelection
        ShowCategoryChooser()
    End Sub

    Private Sub CriteriaSelectionText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles CriteriaSelectionText.Tapped
        ChooserMode = ChooserModes.ForCriteriaSelection
        ShowCategoryChooser()
    End Sub

#End Region

#Region "Timers"
    Private Sub ShowTimers_Click(sender As Object, e As RoutedEventArgs) Handles ShowTimers.Click
        TimerController.TimersPaneOpen = Not TimerController.TimersPaneOpen
    End Sub

    Private Sub ShowTimersText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles ShowTimersText.Tapped
        TimerController.TimersPaneOpen = Not TimerController.TimersPaneOpen
    End Sub
#End Region

#Region "History"
    Private Sub ShowHistory_Click(sender As Object, e As RoutedEventArgs) Handles ShowHistory.Click
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), History.FolderName)
    End Sub

    Private Sub ShowHistoryText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles ShowHistoryText.Tapped
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), History.FolderName)
    End Sub
#End Region

#Region "Galery"
    Private Sub ShowImageGalery_Click(sender As Object, e As RoutedEventArgs) Handles ShowImageGalery.Click
        If _lastSelectedItem IsNot Nothing Then
            Me.Frame.Navigate(GetType(RecipeImageGalery), _lastSelectedItem.GetKey(CurrentRecipeFolder.Name))
        End If
    End Sub
#End Region

#Region "RenameRecipe"
    Private Async Sub RenameRecipe_Click(sender As Object, e As RoutedEventArgs) Handles RenameRecipe.Click

        If _lastSelectedItem Is Nothing Then
            Return
        End If

        Dim oldName As String = _lastSelectedItem.Name

        DisableControls(False)

        Dim nameEditor = New RecipeNameEditor
        nameEditor.SetAction(RecipeNameEditor.FileActions.Rename)
        nameEditor.SetCategory(CurrentRecipeFolder)
        nameEditor.SetTitle(oldName)
        Await nameEditor.ShowAsync()

        If Not nameEditor.DialogCancelled() Then
            Dim newName = nameEditor.GetRecipeTitle()
            If Not oldName.Equals(newName) Then
                Await RecipeFolders.Current.RenameRecipeAsync(_lastSelectedItem, newName)
                pageTitle.Text = newName
            End If
        End If

        EnableControls()
    End Sub

#End Region

#Region "EditRecipe"

    Private Sub EditFileButton_Click(sender As Object, e As RoutedEventArgs) Handles EditFileButton.Click
        If _lastSelectedItem IsNot Nothing Then
            Me.Frame.Navigate(GetType(RecipeEditor), _lastSelectedItem.GetKey(CurrentRecipeFolder.Name))
        End If
    End Sub

#End Region

#Region "Help"
    Private Sub AppHelp_Click(sender As Object, e As RoutedEventArgs) Handles AppHelp.Click
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), HelpDocuments.FolderName)
    End Sub

    Private Sub AppHelpText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles AppHelpText.Tapped
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), HelpDocuments.FolderName)
    End Sub

#End Region

#Region "EditCalories"
    Private Async Sub ChangeCalories_Click(sender As Object, e As RoutedEventArgs) Handles ChangeCalories.Click
        If _lastSelectedItem IsNot Nothing Then
            Dim dialog = New EditCaloriesDialog(_lastSelectedItem)
            Await dialog.ShowAsync()
        End If
    End Sub
#End Region

#Region "EditTags"
    Private Async Sub EditTags_Clicked(sender As Object, e As RoutedEventArgs) Handles EditTags.Click
        Await _lastSelectedItem.AddTag()
    End Sub
#End Region

End Class
