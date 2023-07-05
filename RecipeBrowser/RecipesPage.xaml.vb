' Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Imports RecipeBrowser
Imports Windows.ApplicationModel.DataTransfer
Imports Windows.Foundation.Metadata
Imports Windows.Globalization.DateTimeFormatting
Imports Windows.Storage
Imports Windows.Storage.Streams
Imports Windows.UI.Core
Imports Windows.UI.Popups
Imports Windows.UI.Xaml.Media.Animation
''' <summary>
''' Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
''' </summary>
Public NotInheritable Class RecipesPage
    Inherits Page

    Public Property CurrentRecipeFolder As RecipeFolder
    Public Property OtherCategoryList As New ObservableCollection(Of FolderDescriptor)

    Public Property BackButtonVisibility As Visibility

    Public Property TimerController As Timers.Controller

    Public Shared Property Current As RecipesPage

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
        Current = Me
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

        Dim category = DirectCast(e.NavigationParameter, String)  ' May be a path: "Brunch\Eierspeisen"

        Try
            DisableControls(False) ' do not show action progress

            LoadProgress.Visibility = Visibility.Visible
            LoadProgressDeterminate.Visibility = Visibility.Visible
            LoadProgressDeterminate.Value = 0
            CurrentRecipeFolder = categories.GetFolder(category)
            If e.PageState IsNot Nothing Then
                If CurrentRecipeFolder.Name = History.FolderName AndAlso e.PageState.ContainsKey("HistoryStartDate") Then
                    History.Current.SelectionEndDate = DirectCast(e.PageState("HistoryStartDate"), Date)
                ElseIf CurrentRecipeFolder.Name = LastAddedFolder.FolderName Then
                    LastAddedFolder.Current.SetSearchParameter(DirectCast(e.PageState("LastAddedDate"), DateTime))
                End If
            End If
            If Not CurrentRecipeFolder.ContentLoaded Then
                Await CurrentRecipeFolder.LoadAsync()
            End If
            LoadProgress.Visibility = Visibility.Collapsed
            LoadProgressDeterminate.Visibility = Visibility.Collapsed

            If CurrentRecipeFolder.ContentIsGrouped Then
                GroupedRecipesCVS.Source = CurrentRecipeFolder.GroupedRecipes
                MasterListView.ItemsSource = GroupedRecipesCVS.View
            Else
                MasterListView.ItemsSource = CurrentRecipeFolder.Recipes
            End If

            pageTitle.Text = CurrentRecipeFolder.DisplayName

            If ApiInformation.IsPropertyPresent("Windows.UI.Xaml.FrameworkElement", "AllowFocusOnInteraction") Then
                editNote.AllowFocusOnInteraction = True ' Otherwise the note editor does not accept input
            End If

            AddToFavorites.Visibility = Visibility.Collapsed
            RemoveFromFavorites.Visibility = Visibility.Collapsed
            ShowFavoritesButton.Visibility = CurrentRecipeFolder.FavoritesVisibility
            changeSortOrder.Visibility = CurrentRecipeFolder.ChangeSortOrderVisibility
            EditTags.Visibility = CurrentRecipeFolder.EditTagsVisibility
            RecipeAutoSuggestBox.Visibility = CurrentRecipeFolder.RecipeSearchboxVisibility
            RecipeAutoSuggestBox.IsEnabled = CurrentRecipeFolder.RecipeSearchBoxEnabled
            setFilter.Visibility = CurrentRecipeFolder.SetFilterVisibility
            deleteFilter.Visibility = CurrentRecipeFolder.SetFilterVisibility
            ShowHistoryButton.Visibility = CurrentRecipeFolder.HistoryVisibility
            SearchBoxSpaceHolder.Width = CurrentRecipeFolder.SearchBoxSpaceHolderWidth
            ChangeCategory.Visibility = CurrentRecipeFolder.ChangeCategoryVisibility
            logAsCooked.Visibility = CurrentRecipeFolder.LogAsCookedVisibility
            ShowImageGalery.Visibility = CurrentRecipeFolder.ShowImageGalleryVisibility
            AddExternalRecipeButton.Visibility = CurrentRecipeFolder.AddExternalRecipeVisibility
            AddRecipeButton.Visibility = CurrentRecipeFolder.AddRecipeVisibility
            ExportImportButton.Visibility = CurrentRecipeFolder.ExportImportVisibility
            OpenFile.Visibility = CurrentRecipeFolder.OpenFileVisibility
            ScanForCalories.Visibility = CurrentRecipeFolder.ScanForCaloriesVisibility
            Share.Visibility = CurrentRecipeFolder.ShareVisibility
            deleteRecipe.Visibility = CurrentRecipeFolder.DeleteRecipeVisibility
            RenameRecipe.Visibility = CurrentRecipeFolder.DeleteRecipeVisibility
            ChangeCalories.Visibility = CurrentRecipeFolder.ChangeCaloriesVisibility
            AppHelpButton.Visibility = CurrentRecipeFolder.HelpVisibility
            LastAddedSearchButton.Visibility = CurrentRecipeFolder.LastAddedSearchVisibility

            ' Set ItemTemplate
            If category = Favorites.FolderName Then
                MasterListView.ItemTemplate = DirectCast(Resources("FavoritesItemListTemplate"), DataTemplate)
            ElseIf category = History.FolderName Then
                If AdaptiveStates.CurrentState.Equals(PhoneState) Then
                    MasterListView.ItemTemplate = DirectCast(Resources("NarrowHistoryItemListTemplate"), DataTemplate)
                Else
                    MasterListView.ItemTemplate = DirectCast(Resources("HistoryItemListTemplate"), DataTemplate)
                End If
            ElseIf category = HelpDocuments.FolderName Then
                MasterListView.ItemTemplate = DirectCast(Resources("HelpItemListTemplate"), DataTemplate)
            Else
                MasterListView.ItemTemplate = DirectCast(Resources("RecipeItemListTemplate"), DataTemplate)
            End If

            If EditTags.Visibility = Visibility.Visible AndAlso TagRepository.Current.Directory.Count = 0 Then
                EditTags.Visibility = Visibility.Collapsed 'No tags defined
            End If

            If category = SearchResults.FolderName Then
                'RecipeSearchBox.QueryText = categories.SearchResultsFolder.LastSearchString
                RecipeAutoSuggestBox.Text = categories.SearchResultsFolder.LastSearchString
            End If

            EnableControls()

            'If CurrentRecipeFolder.Folder IsNot Nothing Then
            '    If App.SearchBoxIsSupported Then
            '        Try
            '            Dim searchSuggestions = New Windows.ApplicationModel.Search.LocalContentSuggestionSettings()
            '            searchSuggestions.Enabled = True
            '            searchSuggestions.Locations.Add(CurrentRecipeFolder.Folder)
            '            RecipeSearchBox.SetLocalContentSuggestionSettings(searchSuggestions)
            '        Catch ex As Exception
            '        End Try
            '    End If
            'End If

            MasterListView.SelectedIndex = -1
            If e.PageState IsNot Nothing Then
                ' Den zuvor gespeicherten Zustand wiederherstellen, der dieser Seite zugeordnet ist
                If e.PageState.ContainsKey("SelectedItem") Then
                    Dim selectedItemCategory = DirectCast(e.PageState("SelectedItemCategory"), String)
                    Dim selectedItem = Await categories.GetRecipeAsync(category, selectedItemCategory, DirectCast(e.PageState("SelectedItem"), String))
                    If selectedItem IsNot Nothing Then
                        MasterListView.SelectedItem = selectedItem
                        MasterListView.UpdateLayout()
                        MasterListView.ScrollIntoView(selectedItem, ScrollIntoViewAlignment.Leading)
                    End If
                End If
            End If

            'Await Timers.Factory.Current.WakeUp()
            Await DisplayCurrentItemDetail()
        Catch ex As Exception
            App.Logger.WriteException("RecipesPage.NavigationHelper_LoadState", ex)
        End Try

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
        If _lastSelectedItem IsNot Nothing Then
            Dim itemTitle = _lastSelectedItem.Name
            e.PageState("SelectedItem") = itemTitle
            Dim itemCategory = _lastSelectedItem.Category
            e.PageState("SelectedItemCategory") = itemCategory
        End If

        If CurrentRecipeFolder.Name = History.FolderName Then
            e.PageState("HistoryStartDate") = History.Current.SelectionEndDate
        End If

        If CurrentRecipeFolder.Name = LastAddedFolder.FolderName Then
            e.PageState("LastAddedDate") = LastAddedFolder.Current.LastAddedSince
        End If

    End Sub

    Public Sub SetLoadProgress(value As Double)
        LoadProgressDeterminate.Value = value
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

#Region "AdaptiveStates"

    Private Sub AdaptiveStates_CurrentStateChanged(sender As Object, e As VisualStateChangedEventArgs)

        UpdateForVisualState(e.NewState, e.OldState)
    End Sub

    Private Sub UpdateForVisualState(newState As VisualState, oldState As VisualState)

        Dim isNarrow As Boolean = (newState.Equals(NarrowState) Or newState.Equals(PhoneState))

        If (isNarrow And oldState.Equals(DefaultState) And _lastSelectedItem IsNot Nothing) Then
            ' Resize down to the detail item. Don't play a transition.
            Frame.Navigate(GetType(RecipePage), _lastSelectedItem.GetKey(CurrentRecipeFolder.Name), New SuppressNavigationTransitionInfo())
        End If

        EntranceNavigationTransitionInfo.SetIsTargetElement(MasterListView, isNarrow)
        If DetailContentPresenter IsNot Nothing Then
            EntranceNavigationTransitionInfo.SetIsTargetElement(DetailContentPresenter, Not isNarrow)
        End If
    End Sub

    Private Sub LayoutRoot_Loaded(sender As Object, e As RoutedEventArgs)
        'Assure we are displaying the correct item. This Is necessary in certain adaptive cases.
        MasterListView.SelectedItem = _lastSelectedItem
    End Sub

    Private Sub EnableContentTransitions()

        DetailContentPresenter.ContentTransitions.Clear()
        DetailContentPresenter.ContentTransitions.Add(New EntranceThemeTransition())
    End Sub

    Private Sub DisableContentTransitions()

        If DetailContentPresenter IsNot Nothing Then
            DetailContentPresenter.ContentTransitions.Clear()
        End If
    End Sub

#End Region

#Region "MasterDetailHandling"
    Private Async Function DisplayCurrentItemDetail() As Task

        Dim selectedItem As Recipe = MasterListView.SelectedItem

        Try
            If selectedItem Is Nothing Then
                _lastSelectedItem = Nothing
                TagsList.ItemsSource = Nothing
            ElseIf selectedItem.ItemType = Recipe.ItemTypes.Recipe OrElse selectedItem.ItemType = Recipe.ItemTypes.ExternalRecipe Then
                _lastSelectedItem = MasterListView.SelectedItem
                _lastSelectedItem.LoadTags()
                TagsList.ItemsSource = _lastSelectedItem.Tags
            End If

            If AdaptiveStates.CurrentState.Equals(DefaultState) Then
                If selectedItem IsNot Nothing Then
                    If selectedItem.File IsNot Nothing Then
                        ' Display PDF file
                        Await selectedItem.LoadRecipeAsync()
                        itemDetail.Visibility = Visibility.Visible
                        RecipeViewer.Visibility = Visibility.Visible
                        RecipeSourceViewer.Visibility = Visibility.Collapsed
                        RecipeViewer.Source = selectedItem.RenderedPage.Image
                    ElseIf selectedItem.RecipeSource IsNot Nothing Then
                        ' Display RTF file
                        itemDetail.Visibility = Visibility.Visible
                        RecipeViewer.Visibility = Visibility.Collapsed
                        RecipeSourceViewer.Visibility = Visibility.Visible
                        Try
                            Dim randAccStream As IRandomAccessStream = Await selectedItem.RecipeSource.OpenAsync(FileAccessMode.Read)
                            RecipeSourceViewer.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, randAccStream)
                        Catch ex As Exception
                        End Try
                    Else
                        ' Display image from galery
                        RecipeSourceViewer.Visibility = Visibility.Collapsed
                        Dim folder As RecipeFolder = Await RecipeFolders.Current.GetFolderAsync(selectedItem.Category)
                        If folder IsNot Nothing Then
                            Await folder.GetImagesOfRecipeAsync(selectedItem)
                            If selectedItem.Pictures IsNot Nothing AndAlso selectedItem.Pictures.Count > 0 Then
                                RecipeViewer.Source = selectedItem.Pictures(0).Image
                            Else
                                RecipeViewer.Source = Nothing
                            End If
                        Else
                            RecipeViewer.Source = Nothing
                        End If
                        If RecipeViewer.Source Is Nothing Then
                            itemDetail.Visibility = Visibility.Collapsed
                        Else
                            itemDetail.Visibility = Visibility.Visible
                        End If
                    End If
                Else
                    RecipeViewer.Source = Nothing
                    RecipeSourceViewer.Visibility = Visibility.Collapsed
                    itemDetail.Visibility = Visibility.Collapsed
                End If
            End If
            EnableControls()
        Catch ex As Exception
            App.Logger.WriteException("RecipesPage.DisplayCurrentItemDetail", ex)
        End Try
    End Function

    Private Async Function SelectRecipe(selectedRecipe As Recipe) As Task
        MasterListView.SelectedItem = selectedRecipe

        Select Case selectedRecipe.ItemType
            Case Recipe.ItemTypes.Recipe, Recipe.ItemTypes.ExternalRecipe
                _lastSelectedItem = selectedRecipe

                If AdaptiveStates.CurrentState.Equals(NarrowState) OrElse AdaptiveStates.CurrentState.Equals(PhoneState) Then
                    ' Use "drill in" transition for navigating from master list to detail view
                    Frame.Navigate(GetType(RecipePage), selectedRecipe.GetKey(CurrentRecipeFolder.Name), New DrillInNavigationTransitionInfo())
                Else
                    ' Play a refresh animation when the user switches detail items.
                    Await DisplayCurrentItemDetail()
                    EnableContentTransitions()
                End If

            Case Recipe.ItemTypes.SubCategory
                Frame.Navigate(GetType(RecipesPage), selectedRecipe.Path, New DrillInNavigationTransitionInfo())

            Case Recipe.ItemTypes.Header
                Dim item As Recipe
                If MasterListView.SelectedIndex > 0 Then
                    item = MasterListView.Items(MasterListView.SelectedIndex - 1)
                End If
                Await History.Current.SelectMoreRecipes(False)
                If item IsNot Nothing Then
                    MasterListView.ScrollIntoView(item, ScrollIntoViewAlignment.Leading)
                End If

        End Select

    End Function

    Private Async Sub ItemListView_SelectionChanged(sender As Object, e As ItemClickEventArgs)

        Dim clickedItem As Recipe = DirectCast(e.ClickedItem, Recipe)

        Await SelectRecipe(clickedItem)

    End Sub

    Private Async Sub ChooseRandomRecipe_Click(sender As Object, e As RoutedEventArgs)
        Dim item As Recipe = CurrentRecipeFolder.GetRandomRecipe()
        If item IsNot Nothing Then
            Await SelectRecipe(item)
            MasterListView.ScrollIntoView(item, ScrollIntoViewAlignment.Leading)
        End If
    End Sub
#End Region

#Region "ItemSorter"
    Private Sub SetSortOrder(ByRef sortOrder As RecipeFolder.SortOrder)

        CurrentRecipeFolder.SetSortOrder(sortOrder)
        If CurrentRecipeFolder.ContentIsGrouped Then
            GroupedRecipesCVS.Source = CurrentRecipeFolder.GroupedRecipes
            MasterListView.ItemsSource = GroupedRecipesCVS.View
        Else
            MasterListView.ItemsSource = CurrentRecipeFolder.Recipes
        End If

        If _lastSelectedItem IsNot Nothing Then
            MasterListView.SelectedItem = _lastSelectedItem
        End If
    End Sub

    Private Sub SortNameAscending_Click(sender As Object, e As RoutedEventArgs)

        SetSortOrder(RecipeFolder.SortOrder.ByNameAscending)

    End Sub

    Private Sub SortDateDecending_Click(sender As Object, e As RoutedEventArgs)

        SetSortOrder(RecipeFolder.SortOrder.ByDateDescending)

    End Sub

    Private Sub SortLastCookedDescending_Click(sender As Object, e As RoutedEventArgs)

        SetSortOrder(RecipeFolder.SortOrder.ByLastCookedDescending)

    End Sub

#End Region

#Region "PageControl"
    Private Sub RenderPageControl(ByRef CurrentRecipe As Recipe)
        'visibility
        If CurrentRecipe Is Nothing OrElse CurrentRecipe.RenderedPage Is Nothing Then
            pageControl.Visibility = Visibility.Collapsed
        ElseIf App.ShowZoomButtons Then
            pageControl.Visibility = If(CurrentRecipe.NoOfPages > 1, Visibility.Visible, If(CurrentRecipe.RenderedPage.RenderingOptions IsNot Nothing, Visibility.Visible, Visibility.Collapsed))
        Else
            pageControl.Visibility = If(CurrentRecipe.NoOfPages > 1, Visibility.Visible, Visibility.Collapsed)
        End If

        If pageControl.Visibility = Visibility.Collapsed Then
            Return
        End If

        ' paging
        If CurrentRecipe.NoOfPages > 1 Then
            pageNumber.Text = CurrentRecipe.CurrentPage.ToString + "/" + CurrentRecipe.NoOfPages.ToString
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
            pageNumber.Visibility = Visibility.Visible
            prevPage.Visibility = Visibility.Visible
            nextPage.Visibility = Visibility.Visible
        Else
            pageNumber.Visibility = Visibility.Collapsed
            prevPage.Visibility = Visibility.Collapsed
            nextPage.Visibility = Visibility.Collapsed
        End If

        ' separator
        If CurrentRecipe.NoOfPages > 1 AndAlso App.ShowZoomButtons Then
            pageControlSeparator.Visibility = Visibility.Visible
        Else
            pageControlSeparator.Visibility = Visibility.Collapsed
        End If

        ' zooming
        If App.ShowZoomButtons AndAlso CurrentRecipe.RenderedPage.RenderingOptions IsNot Nothing Then
            enlarge.Visibility = Visibility.Visible
            shrink.Visibility = Visibility.Visible
        Else
            enlarge.Visibility = Visibility.Collapsed
            shrink.Visibility = Visibility.Collapsed
        End If

    End Sub

    Private Async Sub GotoPreviousPage(sender As Object, e As RoutedEventArgs) Handles prevPage.Click

        If _lastSelectedItem Is Nothing Then
            Return
        End If
        DisableControls()
        Await _lastSelectedItem.PreviousPage()
        RecipeViewer.Source = _lastSelectedItem.RenderedPage.Image
        EnableControls()

    End Sub

    Private Async Sub GotoNextPage(sender As Object, e As RoutedEventArgs) Handles nextPage.Click

        If _lastSelectedItem Is Nothing Then
            Return
        End If
        DisableControls()
        Await _lastSelectedItem.NextPage()
        RecipeViewer.Source = _lastSelectedItem.RenderedPage.Image
        EnableControls()
    End Sub

    Private Async Sub ZoomIn(sender As Object, e As RoutedEventArgs) Handles enlarge.Click
        _lastSelectedItem.RenderingOptions.DestinationHeight *= 1.5
        _lastSelectedItem.RenderingOptions.DestinationWidth *= 1.5
        Await _lastSelectedItem.RenderPageAsync()
        RecipeViewer.Source = _lastSelectedItem.RenderedPage.Image
    End Sub

    Private Async Sub ZoomOut(sender As Object, e As RoutedEventArgs) Handles shrink.Click
        _lastSelectedItem.RenderingOptions.DestinationHeight /= 1.5
        _lastSelectedItem.RenderingOptions.DestinationWidth /= 1.5
        Await _lastSelectedItem.RenderPageAsync()
        RecipeViewer.Source = _lastSelectedItem.RenderedPage.Image
    End Sub

    Private Sub RecipeViewer_ManipulationStarted(sender As Object, e As ManipulationStartedRoutedEventArgs)
        RecipeViewer.Opacity = 0.4
    End Sub

    Private Sub RecipeViewer_ManipulationDelta(sender As Object, e As ManipulationDeltaRoutedEventArgs)
        RecipeViewer_Transform.TranslateX += e.Delta.Translation.X
        RecipeViewer_Transform.TranslateY += e.Delta.Translation.Y
    End Sub

    Private Sub RecipeViewer_ManipulationCompleted(sender As Object, e As ManipulationCompletedRoutedEventArgs)
        RecipeViewer.Opacity = 1
    End Sub

#End Region

#Region "EnableDisableControls"

    Private Sub SetEditNoteIcon()
        Dim icon_message As Integer = &HE8BD
        Dim icon_goto_message As Integer = &HF716
        Dim icon As Integer
        If _lastSelectedItem Is Nothing OrElse (_lastSelectedItem.Notes Is Nothing AndAlso _lastSelectedItem.InternetLink Is Nothing) Then
            icon = icon_message
        Else
            icon = icon_goto_message
        End If
        Dim chars As Char() = {ChrW(icon)}
        Dim fontIcon As FontIcon = editNote.Icon
        fontIcon.Glyph = chars
    End Sub

    Private Sub DisableControls(Optional visualizeProgress As Boolean = True)

        ShowFavorites.IsEnabled = False
        Home.IsEnabled = False
        ShowHistory.IsEnabled = False
        refreshRecipes.IsEnabled = False
        FolderSelection.IsEnabled = False
        CriteriaSelection.IsEnabled = False
        ShowTimers.IsEnabled = False
        ScanForCalories.IsEnabled = False
        EditTags.IsEnabled = False
        ShowLastAdded.IsEnabled = False
        If visualizeProgress Then
            actionProgress.IsActive = True
        End If
        'RecipeSearchBox.IsEnabled = False
        RecipeAutoSuggestBox.IsEnabled = False
        nextPage.IsEnabled = False
        prevPage.IsEnabled = False
        AddToFavorites.IsEnabled = False
        RemoveFromFavorites.IsEnabled = False
        OpenFile.IsEnabled = False
        ChangeCategory.IsEnabled = False
        'Menu.IsEnabled = False
        deleteRecipe.IsEnabled = False
        RenameRecipe.IsEnabled = False
        editNote.IsEnabled = False
        EditFileButton.IsEnabled = False
        logAsCooked.IsEnabled = False
        FullscreenView.IsEnabled = False
        Share.IsEnabled = False
        setFilter.IsEnabled = False
        deleteFilter.IsEnabled = False
        ShowImageGalery.IsEnabled = False
        AddExternalRecipe.IsEnabled = False
        AddRecipe.IsEnabled = False
        ExportImport.IsEnabled = False
        ChangeCalories.IsEnabled = False
        'MasterListView.IsItemClickEnabled = False
    End Sub

    Private Sub EnableControls()

        ShowFavorites.IsEnabled = True
        Home.IsEnabled = True
        actionProgress.IsActive = False
        refreshRecipes.IsEnabled = True
        ShowHistory.IsEnabled = True
        ShowLastAdded.IsEnabled = True
        FolderSelection.IsEnabled = True
        CriteriaSelection.IsEnabled = True
        ShowTimers.IsEnabled = True
        ScanForCalories.IsEnabled = True
        AddRecipe.IsEnabled = True
        AddExternalRecipe.IsEnabled = True
        ExportImport.IsEnabled = True
        EditTags.IsEnabled = True
        RecipeAutoSuggestBox.IsEnabled = CurrentRecipeFolder.RecipeSearchBoxEnabled
        ExportHistoryMenuItem.IsEnabled = History.Current.IsInitialized

        RenderPageControl(_lastSelectedItem) ' currentRecipe may be nothing

        SetEditNoteIcon()

        ' Functions for the current recipe
        If _lastSelectedItem Is Nothing Then
            ' Hide if nothing is selected
            AddToFavorites.IsEnabled = False
            RemoveFromFavorites.IsEnabled = False
            OpenFile.IsEnabled = False
            ChangeCategory.IsEnabled = False
            ChangeCalories.IsEnabled = False
            deleteRecipe.IsEnabled = False
            RenameRecipe.IsEnabled = False
            logAsCooked.IsEnabled = False
            EditTags.IsEnabled = False
            FullscreenView.IsEnabled = False
            editNote.Label = ""
            editNote.IsEnabled = False
            editNote.SetValue(ForegroundProperty, Current.Resources("AppBarItemDisabledForegroundThemeBrush"))
            Share.IsEnabled = False
            ShowImageGalery.IsEnabled = False
            EditFileButton.IsEnabled = False
        Else
            Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

            ShowImageGalery.IsEnabled = True
            RenameRecipe.IsEnabled = True
            ChangeCategory.IsEnabled = True
            ChangeCalories.IsEnabled = True

            ' Fullscreen view enabled if any content is available: rendered pdf, rtf, image
            If _lastSelectedItem.RenderedPage IsNot Nothing OrElse _lastSelectedItem.RecipeSource IsNot Nothing OrElse
                   (_lastSelectedItem.Pictures IsNot Nothing AndAlso _lastSelectedItem.Pictures.Count > 0) Then
                FullscreenView.IsEnabled = True
            Else
                FullscreenView.IsEnabled = False
            End If

            deleteRecipe.IsEnabled = _lastSelectedItem.ItemType = Recipe.ItemTypes.Recipe
            logAsCooked.IsEnabled = Not _lastSelectedItem.CookedToday()

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
                        editNote.SetValue(ForegroundProperty, App.Current.Resources("AppBarButtonForegroundBrush"))
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
                ' Recipe content (pdf or rtf) is not available
                editNote.IsEnabled = False
                EditFileButton.IsEnabled = False
                OpenFile.IsEnabled = False
                Share.IsEnabled = False
            End If

            'Favorites handling (not on templates and help page, toggle add and remove)
            AddToFavorites.IsEnabled = True
            RemoveFromFavorites.IsEnabled = True
            If CurrentRecipeFolder.Name = RecipeTemplates.FolderName Or
                   CurrentRecipeFolder.Name = HelpDocuments.FolderName Then
                RemoveFromFavorites.Visibility = Visibility.Collapsed
                AddToFavorites.Visibility = Visibility.Collapsed
            ElseIf CurrentRecipeFolder.Name = Favorites.FolderName OrElse categories.FavoriteFolder.IsFavorite(_lastSelectedItem) Then
                RemoveFromFavorites.Visibility = Visibility.Visible
                AddToFavorites.Visibility = Visibility.Collapsed
            Else
                RemoveFromFavorites.Visibility = Visibility.Collapsed
                AddToFavorites.Visibility = Visibility.Visible
            End If
        End If

        If CurrentRecipeFolder.Name = History.FolderName Then
            setFilter.IsEnabled = True
            deleteFilter.IsEnabled = History.Current.CategoryFilter <> String.Empty
        End If

    End Sub

#End Region

#Region "SearchBox"
    Private Sub SearchBox_QuerySubmitted(sender As SearchBox, args As SearchBoxQuerySubmittedEventArgs)

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        If TypeOf CurrentRecipeFolder Is TagFolder Then
            categories.SearchResultsFolder.SetSearchParameter("", args.QueryText, SearchTag:=DirectCast(CurrentRecipeFolder, TagFolder).Tag.Tag)
        ElseIf TypeOf CurrentRecipeFolder Is LastAddedFolder Then
            categories.SearchResultsFolder.SetSearchParameter("", args.QueryText, SearchAddedDate:=LastAddedFolder.Current.CurrentAddedSince)
        Else
            categories.SearchResultsFolder.SetSearchParameter(CurrentRecipeFolder.Name, args.QueryText)
        End If

        Me.Frame.Navigate(GetType(RecipesPage), SearchResults.FolderName)
    End Sub

    Private Async Sub RecipeAutoSuggestBox_QuerySubmitted(sender As AutoSuggestBox, args As AutoSuggestBoxQuerySubmittedEventArgs) Handles RecipeAutoSuggestBox.QuerySubmitted

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        If args.ChosenSuggestion Is Nothing Then
            If TypeOf CurrentRecipeFolder Is TagFolder Then
                categories.SearchResultsFolder.SetSearchParameter("", args.QueryText, SearchTag:=DirectCast(CurrentRecipeFolder, TagFolder).Tag.Tag)
            ElseIf TypeOf CurrentRecipeFolder Is LastAddedFolder Then
                categories.SearchResultsFolder.SetSearchParameter("", args.QueryText, SearchAddedDate:=LastAddedFolder.Current.CurrentAddedSince)
            Else
                categories.SearchResultsFolder.SetSearchParameter(CurrentRecipeFolder.Name, args.QueryText)
            End If
            Me.Frame.Navigate(GetType(RecipesPage), SearchResults.FolderName)
        Else
            Dim chosen As Recipe = args.ChosenSuggestion
            Await SelectRecipe(chosen)
            MasterListView.ScrollIntoView(chosen)
        End If

    End Sub

    Private Sub RecipeAutoSuggestBox_TextChanged(sender As AutoSuggestBox, args As AutoSuggestBoxTextChangedEventArgs) Handles RecipeAutoSuggestBox.TextChanged

        'We only want to get results when it was a user typing, 
        'otherwise we assume the value got filled in by TextMemberPath 
        'Or the handler for SuggestionChosen
        If args.Reason = AutoSuggestionBoxTextChangeReason.UserInput Then
            Dim matchincRecipes = CurrentRecipeFolder.GetMatchingRecipes(sender.Text)
            sender.ItemsSource = matchincRecipes.ToList()
        End If
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
    Private Async Sub OpenRecipe_Click(sender As Object, e As RoutedEventArgs) Handles OpenFile.Click

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

    Private Sub FullscreenView_Click(sender As Object, e As RoutedEventArgs) Handles FullscreenView.Click

        If _lastSelectedItem IsNot Nothing Then
            Frame.Navigate(GetType(RecipePage), _lastSelectedItem.GetKey(CurrentRecipeFolder.Name))
        End If

    End Sub
#End Region

#Region "Favorites"
    Private Async Sub AddToFavorites_Click(sender As Object, e As RoutedEventArgs) Handles AddToFavorites.Click

        If _lastSelectedItem IsNot Nothing Then
            Await AddRecipeToFavorites(_lastSelectedItem)
        End If

    End Sub

    Public Async Function AddRecipeToFavorites(toAdd As Recipe) As Task
        DisableControls()

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
        Await categories.FavoriteFolder.AddRecipeAsync(toAdd)
        EnableControls()
    End Function

    Private Async Sub RemoveFromFavorites_Click(sender As Object, e As RoutedEventArgs) Handles RemoveFromFavorites.Click

        If _lastSelectedItem IsNot Nothing Then
            Await RemoveRecipeFromFavorites(_lastSelectedItem)
        End If

    End Sub

    Public Async Function RemoveRecipeFromFavorites(toRemove As Recipe) As Task
        Dim categories As RecipeFolders = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
        If Not categories.FavoriteFolder.ContentLoaded Then
            DisableControls(True)
            Await categories.FavoriteFolder.LoadAsync()
        End If
        categories.FavoriteFolder.DeleteRecipe(toRemove)
        If CurrentRecipeFolder.Name = Favorites.FolderName AndAlso _lastSelectedItem IsNot Nothing AndAlso _lastSelectedItem.Equals(toRemove) Then
            RecipeViewer.Source = Nothing
            TagsList.ItemsSource = Nothing
            _lastSelectedItem = Nothing
        End If
        EnableControls()
    End Function

    Private Async Sub RecipePin_Execute(sender As SwipeItem, args As SwipeItemInvokedEventArgs)
        Try
            Await AddRecipeToFavorites(args.SwipeControl.DataContext)

        Catch ex As Exception

        End Try

    End Sub

    Private Async Sub RecipeUnpin_Execute(sender As SwipeItem, args As SwipeItemInvokedEventArgs)
        Try
            Await RemoveRecipeFromFavorites(args.SwipeControl.DataContext)

        Catch ex As Exception

        End Try
    End Sub

    Private Sub RecipeItemListSwipeControl_PointerEntered(sender As Object, e As PointerRoutedEventArgs)
        If e.Pointer.PointerDeviceType = Windows.Devices.Input.PointerDeviceType.Mouse OrElse e.Pointer.PointerDeviceType = Windows.Devices.Input.PointerDeviceType.Pen Then
            Dim control = DirectCast(sender, Control)
            DirectCast(control.DataContext, Recipe).PointerEntered = True
        End If
    End Sub

    Private Sub RecipeItemListSwipeControl_PointerExited(sender As Object, e As PointerRoutedEventArgs)
        Dim control = DirectCast(sender, Control)
        DirectCast(control.DataContext, Recipe).PointerEntered = False
    End Sub

#End Region

#Region "Navigation"
    Private Sub ShowFavorites_Click(sender As Object, e As RoutedEventArgs) Handles ShowFavorites.Click
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), Favorites.FolderName)
    End Sub

    Private Sub FavoriteRecipesTextblock_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles FavoriteRecipesTextblock.Tapped
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

    Private Sub HomeTextblock_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles HomeTextblock.Tapped
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(CategoryOverview))
    End Sub

    Private Sub CommandBar_Opening(sender As Object, e As Object)
        Dim cb As CommandBar = sender
        If cb IsNot Nothing Then cb.Background.Opacity = 1.0
    End Sub

    Private Sub CommandBar_Closing(sender As Object, e As Object)
        Dim cb As CommandBar = sender
        If cb IsNot Nothing Then cb.Background.Opacity = 0.5
    End Sub

    Private Sub ToggleSplitView_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles ToggleSplitView.PointerEntered
        ToggleSplitView.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
    End Sub

    Private Sub ToggleSplitView_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles ToggleSplitView.PointerExited
        ToggleSplitView.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
    End Sub

    Private Sub AddExternalRecipeButton_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles AddExternalRecipeButton.PointerEntered
        AddExternalRecipe.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        AddExternalRecipeText.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub AddExternalRecipeButton_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles AddExternalRecipeButton.PointerExited
        AddExternalRecipe.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        AddExternalRecipeText.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub AddRecipeButton_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles AddRecipeButton.PointerEntered
        AddRecipe.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        AddRecipeTextblock.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub AddRecipeButton_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles AddRecipeButton.PointerExited
        AddRecipe.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        AddRecipeTextblock.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub Home_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles HomeButton.PointerExited
        Dim icon_home As Integer = &HE80F
        Dim chars As Char() = {ChrW(icon_home)}
        Home.Content = New String(chars)
        HomeTextblock.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub Home_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles HomeButton.PointerEntered
        Dim icon_home_filled As Integer = &HEA8A
        Dim chars As Char() = {ChrW(icon_home_filled)}
        Home.Content = New String(chars)
        HomeTextblock.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub ShowFavorites_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles ShowFavoritesButton.PointerEntered
        ShowFavorites_Filled.Visibility = Visibility.Visible
        FavoriteRecipesTextblock.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub ShowFavorites_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles ShowFavoritesButton.PointerExited
        ShowFavorites_Filled.Visibility = Visibility.Collapsed
        FavoriteRecipesTextblock.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub LastAddedSearchButton_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles LastAddedSearchButton.PointerEntered
        ShowLastAdded.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        ShowLastAddedText.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub LastAddedSearchButton_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles LastAddedSearchButton.PointerExited
        ShowLastAdded.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        ShowLastAddedText.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub ShowHistoryButton_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles ShowHistoryButton.PointerEntered
        ShowHistory.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        ShowHistoryText.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub ShowHistoryButton_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles ShowHistoryButton.PointerExited
        ShowHistory.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        ShowHistoryText.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub ShowTimersButton_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles ShowTimersButton.PointerEntered
        ShowTimers.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        ShowTimersText.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub ShowTimersButton_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles ShowTimersButton.PointerExited
        ShowTimers.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        ShowTimersText.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub FolderSelectionButton_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles FolderSelectionButton.PointerExited
        Dim icon_folder As Integer = &HF12B
        Dim chars As Char() = {ChrW(icon_folder)}
        FolderSelection.Content = New String(chars)
        FolderSelectionText.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub FolderSelectionButton_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles FolderSelectionButton.PointerEntered
        Dim icon_folder_filled As Integer = &HE8D5
        Dim chars As Char() = {ChrW(icon_folder_filled)}
        FolderSelection.Content = New String(chars)
        FolderSelectionText.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub CriteriaSelectionPane_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles CriteriaSelectionPane.PointerEntered
        CriteriaSelection.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        CriteriaSelectionText.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub CriteriaSelectionPane_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles CriteriaSelectionPane.PointerExited
        CriteriaSelection.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        CriteriaSelectionText.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub RefreshRecipesButton_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles RefreshRecipesButton.PointerEntered
        refreshRecipes.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        RefreshRecipesText.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub RefreshRecipesButton_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles RefreshRecipesButton.PointerExited
        refreshRecipes.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        RefreshRecipesText.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub ExportImportButton_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles ExportImportButton.PointerEntered
        ExportImport.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        ImportExportHistoryText.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub ExportImportButton_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles ExportImportButton.PointerExited
        ExportImport.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        ImportExportHistoryText.FontWeight = Windows.UI.Text.FontWeights.Normal
    End Sub

    Private Sub AppHelpButton_PointerEntered(sender As Object, e As PointerRoutedEventArgs) Handles AppHelpButton.PointerEntered
        AppHelp.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        AppHelpText.FontWeight = Windows.UI.Text.FontWeights.Bold
    End Sub

    Private Sub AppHelpButton_PointerExited(sender As Object, e As PointerRoutedEventArgs) Handles AppHelpButton.PointerExited
        AppHelp.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        AppHelpText.FontWeight = Windows.UI.Text.FontWeights.Normal
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

        Dim selectedRecipe = _lastSelectedItem

        Dim convertedDate As Date = Recipe.ConvertToDate(DateTimeFormatter.ShortDate.Format(CookedOn.Date))

        If History.Current.RecipeWasCookedOn(selectedRecipe.Category, selectedRecipe.Name, convertedDate) Then
            Dim msg = New MessageDialog(App.Texts.GetString("LastCookedDateAlreadyStored"))
            Await msg.ShowAsync()
            Return
        End If

        Await selectedRecipe.LogRecipeCookedAsync(CookedOn.Date)

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
        MasterListView.SelectedItem = Nothing
        Await DisplayCurrentItemDetail()
        EnableControls()

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
            Dim newName As String = nameEditor.GetRecipeTitle()
            If Not oldName.Equals(newName) Then
                Await RecipeFolders.Current.RenameRecipeAsync(_lastSelectedItem, newName)
            End If
        End If

        EnableControls()
    End Sub
#End Region

#Region "RefreshRecipes"
    Private Async Function DoRefreshRecipes() As Task

        LoadProgress.Visibility = Visibility.Visible
        LoadProgressDeterminate.Visibility = Visibility.Visible
        LoadProgressDeterminate.Value = 0
        CurrentRecipeFolder.Invalidate()
        Await RecipeFolders.Current.LoadAsync() ' Reread folder structure
        Await CurrentRecipeFolder.LoadAsync()
        LoadProgress.Visibility = Visibility.Collapsed
        LoadProgressDeterminate.Visibility = Visibility.Collapsed

    End Function

    Private Async Function RefreshRecipesRequested() As Task

        DisableControls(False)
        If CurrentRecipeFolder.Name = History.FolderName Then
            Await History.Current.RescanRepositoryCheck()
        End If
        Await DoRefreshRecipes()
        EnableControls()

    End Function

    Private Async Sub RefreshRecipes_Click(sender As Object, e As RoutedEventArgs) Handles refreshRecipes.Click
        Await RefreshRecipesRequested()
    End Sub

    Private Async Sub RefreshRecipesText_TappedAsync(sender As Object, e As TappedRoutedEventArgs) Handles RefreshRecipesText.Tapped
        Await RefreshRecipesRequested()
    End Sub

#End Region

#Region "Category Chooser"
    Private Enum ChooserModes
        ForChangeCategory
        ForFolderSelection
        ForCategoryFilter
        ForCriteriaSelection
    End Enum

    Private ChooserMode As ChooserModes

    Public Sub ShowCategoryChooser()
        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
        Dim currentCategory As String

        DisableControls(False)

        If _lastSelectedItem Is Nothing Then
            currentCategory = CurrentRecipeFolder.Name
        Else
            currentCategory = _lastSelectedItem.Category
        End If

        If ChooserMode = ChooserModes.ForCriteriaSelection Then
            ' Build a list of criteria. Mark criteria of current recipe
            OtherCategoryList.Clear()
            For Each T In TagRepository.Current.Directory
                Dim folder As New FolderDescriptor With {.CategoryWithIndent = T.Tag, .CategoryPath = T.Tag}
                If (_lastSelectedItem Is Nothing AndAlso currentCategory.Equals(T.Tag)) OrElse
                   (_lastSelectedItem IsNot Nothing AndAlso _lastSelectedItem.HasTag(T.Tag)) Then
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
                If folder.CategoryPath = currentCategory Then
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
            Case ChooserModes.ForFolderSelection Or ChooserModes.ForCriteriaSelection
                CategoryChooserTitle.Visibility = Visibility.Collapsed
            Case ChooserModes.ForChangeCategory
                CategoryChooserTitle.Visibility = Visibility.Visible
                CategoryChooserTitle.Text = App.Texts.GetString("MoveTo.Text")
            Case ChooserModes.ForCategoryFilter
                CategoryChooserTitle.Visibility = Visibility.Visible
                CategoryChooserTitle.Text = App.Texts.GetString("CategoryFilter")
        End Select

        RootSplitView.IsPaneOpen = False
        FolderSelectionSplitView.IsPaneOpen = True
    End Sub

    Private Async Sub Category_Chosen(sender As Object, e As ItemClickEventArgs)
        EnableControls()

        Dim selectedItem = DirectCast(e.ClickedItem, FolderDescriptor)

        If selectedItem IsNot Nothing Then
            FolderSelectionSplitView.IsPaneOpen = False
            Select Case ChooserMode
                Case ChooserModes.ForFolderSelection, ChooserModes.ForCriteriaSelection
                    FolderSelectionChosen(selectedItem)
                Case ChooserModes.ForChangeCategory
                    Await ChangeCategoryOfCurrentItem(selectedItem)
                Case ChooserModes.ForCategoryFilter
                    SetCategoryFilter(selectedItem)
            End Select
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

        Dim selectedRecipe = _lastSelectedItem

        DisableControls()

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
        Dim chosenCategory As RecipeFolder = Await categories.GetFolderAsync(newCategory.CategoryPath)
        Await categories.ChangeCategoryAsync(selectedRecipe, chosenCategory)

        'Did the user select another recipe meanwhile?
        If _lastSelectedItem.Equals(selectedRecipe) Then 'No..
            _lastSelectedItem = Nothing
            Await DisplayCurrentItemDetail()
        End If

        EnableControls()

    End Function
#End Region

#Region "FolderSelection"
    Private Sub FolderSelection_Click(sender As Object, e As RoutedEventArgs) Handles FolderSelection.Click
        ChooserMode = ChooserModes.ForFolderSelection
        ShowCategoryChooser()
    End Sub

    Private Sub FolderSelectionText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles FolderSelectionText.Tapped
        ChooserMode = ChooserModes.ForFolderSelection
        ShowCategoryChooser()
    End Sub

    Private Sub FolderSelectionChosen(chosenCategory As FolderDescriptor)
        If chosenCategory.CategoryPath <> CurrentRecipeFolder.Name Then
            Me.Frame.Navigate(GetType(RecipesPage), chosenCategory.CategoryPath)
        End If
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
        Me.Frame.Navigate(GetType(RecipesPage), History.FolderName)
    End Sub

    Private Sub ShowHistoryText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles ShowHistoryText.Tapped
        Me.Frame.Navigate(GetType(RecipesPage), History.FolderName)
    End Sub

    Private Sub setFilter_Click(sender As Object, e As RoutedEventArgs) Handles setFilter.Click
        ChooserMode = ChooserModes.ForCategoryFilter
        ShowCategoryChooser()
    End Sub

    Private Async Sub SetCategoryFilter(selectedItem As FolderDescriptor)
        DisableControls()
        History.Current.CategoryFilter = selectedItem.CategoryPath
        Await History.Current.LoadAsync()
        EnableControls()
    End Sub

    Private Async Sub deleteFilter_Click(sender As Object, e As RoutedEventArgs) Handles deleteFilter.Click
        DisableControls()
        History.Current.CategoryFilter = String.Empty
        History.Current.SelectionEndDate = Date.Now
        Await History.Current.LoadAsync()
        EnableControls()
    End Sub

    Private Async Sub addExternalRecipe_Click()
        Dim dialog As New RecipeFromExternalSource()
        Await dialog.ShowAsync()
        DisableControls()
        Await History.Current.LoadAsync()
        EnableControls()
    End Sub


#End Region

#Region "Galery"
    Private Sub ShowImageGalery_Click(sender As Object, e As RoutedEventArgs) Handles ShowImageGalery.Click
        If _lastSelectedItem IsNot Nothing Then
            Me.Frame.Navigate(GetType(RecipeImageGalery), _lastSelectedItem.GetKey(CurrentRecipeFolder.Name))
        End If
    End Sub
#End Region

#Region "AddRecipe"
    Private Sub AddExternalRecipe_Click_1(sender As Object, e As RoutedEventArgs) Handles AddExternalRecipe.Click
        addExternalRecipe_Click()
    End Sub

    Private Sub AddExternalRecipe_Tapped(sender As Object, e As TappedRoutedEventArgs)
        addExternalRecipe_Click()
    End Sub

    Private Sub AddRecipeTextblock_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles AddRecipeTextblock.Tapped
        AddRecipeMenuFlyout.ShowAt(AddRecipeButton)
    End Sub


    Private Async Function AddExistingPdf_ClickAsync(sender As Object, e As RoutedEventArgs) As Task Handles AddExistingPdf.Click

        DisableControls(False)

        Dim openPicker = New Windows.Storage.Pickers.FileOpenPicker()
        openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail

        ' Filter to include a sample subset of file types.
        openPicker.FileTypeFilter.Clear()
        If CurrentRecipeFolder.Name <> RecipeTemplates.FolderName Then
            openPicker.FileTypeFilter.Add(".pdf")
        End If
        openPicker.FileTypeFilter.Add(".rtf")

        ' Open the file picker.
        Dim file As StorageFile = Await openPicker.PickSingleFileAsync()

        ' file is null if user cancels the file picker.
        If file IsNot Nothing AndAlso file.Name.Length > 4 Then
            Try
                Dim nameEditor = New RecipeNameEditor()
                nameEditor.SetCategory(CurrentRecipeFolder)
                nameEditor.SetFile(file)
                nameEditor.SetTitle(file.Name.Remove(file.Name.Length - 4))
                nameEditor.SetAction(RecipeNameEditor.FileActions.Copy)

                Await nameEditor.ShowAsync()

                If Not nameEditor.DialogCancelled() Then
                    Await DoRefreshRecipes()
                End If

            Catch ex As Exception
            End Try
        End If

        EnableControls()

    End Function

    Private Async Function EnterNewRecipe_ClickAsync(sender As Object, e As RoutedEventArgs) As Task Handles EnterNewRecipe.Click


        ' Make sure the templates folder is loaded
        Await RecipeFolders.Current.GetFolderAsync(RecipeTemplates.FolderName)

        Dim newRecipeDialog = New RecipeNameEditor
        newRecipeDialog.SetCategory(CurrentRecipeFolder)
        newRecipeDialog.SetAction(RecipeNameEditor.FileActions.NewRecipe)

        Await newRecipeDialog.ShowAsync()

        If newRecipeDialog.DialogCancelled() Then
            Return
        End If

        Dim template = newRecipeDialog.GetRecipeTemplate()
        Dim title = newRecipeDialog.GetRecipeTitle()

        Try
            Dim newFile As StorageFile = Await CurrentRecipeFolder.Folder.CreateFileAsync(title + ".rtf")
            If template IsNot Nothing Then
                ' Copy the content rather than the template file itself so that the creation date is not taken from the template
                Dim inBuffer As IBuffer = Await FileIO.ReadBufferAsync(template.RecipeSource)
                Await FileIO.WriteBufferAsync(newFile, inBuffer)
            End If
        Catch ex As Exception
            App.Logger.Write("Could not create new recipe file: " + ex.ToString())
            Return
        End Try

        Await DoRefreshRecipes()

        Dim newRecipe = CurrentRecipeFolder.GetRecipe(CurrentRecipeFolder.Name, title)

        Dim titleDataPackage As New Windows.ApplicationModel.DataTransfer.DataPackage
        titleDataPackage.SetText(title)
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(titleDataPackage)

        Dim msg = New MessageDialog(App.Texts.GetString("TitleCopiedToClipboard"))
        Await msg.ShowAsync()

        Me.Frame.Navigate(GetType(RecipeEditor), newRecipe.GetKey(CurrentRecipeFolder.Name))

    End Function

    Private Async Function CreateSubCategory_ClickAsync(sender As Object, e As RoutedEventArgs) As Task Handles NewSubCategory.Click

        Dim newRecipeDialog = New RecipeNameEditor
        newRecipeDialog.SetCategory(CurrentRecipeFolder)
        newRecipeDialog.SetAction(RecipeNameEditor.FileActions.NewFolder)

        Await newRecipeDialog.ShowAsync()

        If newRecipeDialog.DialogCancelled() Then
            Return
        End If

        Dim title = newRecipeDialog.GetRecipeTitle()

        Try
            Await CurrentRecipeFolder.Folder.CreateFolderAsync(title)
        Catch ex As Exception
            App.Logger.Write("Could not create new folder: " + ex.ToString())
            Return
        End Try

        Await DoRefreshRecipes()

        EnableControls()
    End Function
#End Region

#Region "ExportImportHistory"

    Private Sub ImportExportHistoryText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles ImportExportHistoryText.Tapped
        ExportImportFlyout.ShowAt(ExportImport)
    End Sub

    Private Async Sub ExportHistory_Click(sender As Object, e As RoutedEventArgs)

        DisableControls(False)

        Await History.Current.ExportHistoryAsync()

        EnableControls()

    End Sub

    Private Async Sub ImportHistory_Click(sender As Object, e As RoutedEventArgs)

        DisableControls(False)

        Await History.Current.ImportHistoryAsync()

        Await DoRefreshRecipes()

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

#Region "SubCategoryOps"
    Private Async Sub RenameSubCategory_Click(sender As Object, e As RoutedEventArgs)

        Dim selectedPath As String = DirectCast(sender, MenuFlyoutItem).CommandParameter
        If selectedPath Is Nothing Then
            Return
        End If

        Dim pos = selectedPath.LastIndexOf("\")
        If pos < 0 Then
            Return
        End If

        Dim oldName As String = selectedPath.Substring(pos + 1)
        If oldName Is Nothing Then
            Return
        End If

        Dim selectedFolder As RecipeFolder = RecipeFolders.Current.GetFolder(selectedPath)
        If selectedFolder Is Nothing Then
            Return
        End If

        DisableControls(False)

        Dim nameEditor = New RecipeNameEditor
        nameEditor.SetAction(RecipeNameEditor.FileActions.RenameFolder)
        nameEditor.SetCategory(CurrentRecipeFolder)
        nameEditor.SetTitle(oldName)
        Await nameEditor.ShowAsync()

        If Not nameEditor.DialogCancelled() Then
            Dim newName As String = nameEditor.GetRecipeTitle()
            If Not oldName.Equals(newName) Then
                Await RecipeFolders.Current.ModifyCategoryAsync(selectedFolder, newName, Nothing)
                Dim entryInRecipesList = CurrentRecipeFolder.GetSubcategory(CurrentRecipeFolder.Name, oldName)
                If entryInRecipesList IsNot Nothing Then
                    entryInRecipesList.Name = newName
                End If
            End If
        End If

        EnableControls()
    End Sub

    Private Async Sub DeleteSubCategory_ClickAsync(sender As Object, e As RoutedEventArgs)

        Dim selectedPath As String = DirectCast(sender, MenuFlyoutItem).CommandParameter
        If selectedPath Is Nothing Then
            Return
        End If

        Dim selectedFolder As RecipeFolder = Await RecipeFolders.Current.GetFolderAsync(selectedPath)
        If selectedFolder Is Nothing Then
            Return
        End If

        Dim messageDialog As Windows.UI.Popups.MessageDialog

        If selectedFolder.Recipes.Count > 0 Then
            messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("FolderNotEmpty"))
            Await messageDialog.ShowAsync()
            Return
        End If

        messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("DoYouWantToDeleteFolder"))

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

        MasterListView.SelectedItem = Nothing

        Await RecipeFolders.Current.DeleteFolderAsync(selectedFolder)
    End Sub

#End Region

#Region "ScanForCalories"

    Enum AskForAllFoldersOptions
        AllFolders
        ThisFolder
        Cancel
    End Enum

    Dim AskForAllFoldersOption As AskForAllFoldersOptions

    Private Async Function AskForAllFolders() As Task

        If CurrentRecipeFolder.Name = Favorites.FolderName OrElse
           CurrentRecipeFolder.Name = History.FolderName OrElse
           CurrentRecipeFolder.Name = SearchResults.FolderName Then
            AskForAllFoldersOption = AskForAllFoldersOptions.ThisFolder
            Return
        End If

        Dim messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("ScanAllFoldersQuestion"))

        ' Add buttons and set their callbacks
        messageDialog.Commands.Add(New UICommand(App.Texts.GetString("ScanAllFolders"), Sub(command)
                                                                                            AskForAllFoldersOption = AskForAllFoldersOptions.AllFolders
                                                                                        End Sub))

        messageDialog.Commands.Add(New UICommand(App.Texts.GetString("ScanThisFolder"), Sub(command)
                                                                                            AskForAllFoldersOption = AskForAllFoldersOptions.ThisFolder
                                                                                        End Sub))

        messageDialog.Commands.Add(New UICommand(App.Texts.GetString("Cancel"), Sub(command)
                                                                                    AskForAllFoldersOption = AskForAllFoldersOptions.Cancel
                                                                                End Sub))

        ' Set the command that will be invoked by default
        messageDialog.DefaultCommandIndex = 1

        ' Set the command to be invoked when escape is pressed
        messageDialog.CancelCommandIndex = 2

        Await messageDialog.ShowAsync()

    End Function

    Private Async Sub ScanForCalories_Click(sender As Object, e As RoutedEventArgs) Handles ScanForCalories.Click

        Await AskForAllFolders()

        If AskForAllFoldersOption = AskForAllFoldersOptions.Cancel Then
            Return
        End If

        Dim scanProgress = New ScanForCaloriesProgressDialog
        If AskForAllFoldersOption = AskForAllFoldersOptions.ThisFolder Then
            scanProgress.SetCategory(CurrentRecipeFolder)
        End If

        Await scanProgress.ShowAsync()
    End Sub

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
