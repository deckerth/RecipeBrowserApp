' Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Imports Windows.Foundation.Metadata
Imports Windows.UI
Imports Windows.UI.Core
Imports Windows.UI.Popups
''' <summary>
''' Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
''' </summary>
Public NotInheritable Class CategoryOverview
    Inherits Page
    Implements INotifyPropertyChanged

    Public Event PropertyChanged(ByVal sender As Object, ByVal e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

    Protected Sub OnPropertyChanged(ByVal PropertyName As String)
        ' Raise the event, and make this procedure
        ' overridable, should someone want to inherit from
        ' this class and override this behavior:
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(PropertyName))
    End Sub

    Dim Categories As RecipeFolders = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

    Public Property TimerController As Timers.Controller

    Private _displayModeCategories As Boolean = True
    Public Property DisplayModeCategories As Boolean
        Get
            Return _displayModeCategories
        End Get
        Set(value As Boolean)
            If value <> _displayModeCategories Then
                _displayModeCategories = value
                OnPropertyChanged("DisplayModeCategories")
            End If
        End Set
    End Property

    Public Shared Current As CategoryOverview
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
        RenderSearchElements(VisualStateGroup.CurrentState)

        Me._navigationHelper = New Common.NavigationHelper(Me)
        AddHandler Me._navigationHelper.LoadState, AddressOf NavigationHelper_LoadState
        AddHandler Me._navigationHelper.SaveState, AddressOf NavigationHelper_SaveState

        TimerController = Timers.Controller.Current
        Current = Me
    End Sub

    ''' <summary>
    ''' Füllt die Seite mit Inhalt auf, der bei der Navigation übergeben wird.  Gespeicherte Zustände werden ebenfalls
    ''' bereitgestellt, wenn eine Seite aus einer vorherigen Sitzung neu erstellt wird.
    ''' </summary>
    ''' <param name="sender">
    ''' Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/>
    ''' </param>
    ''' <param name="e">Ereignisdaten, die die Navigationsparameter bereitstellen, die an
    ''' <see cref="Frame.Navigate"/> übergeben wurde, als diese Seite ursprünglich angefordert wurde und
    ''' ein Wörterbuch des Zustands, der von dieser Seite während einer früheren
    ''' beibehalten wurde.  Der Zustand ist beim ersten Aufrufen einer Seite NULL.</param>
    Private Async Sub NavigationHelper_LoadState(sender As Object, e As Common.LoadStateEventArgs)
        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        If e IsNot Nothing AndAlso e.PageState IsNot Nothing Then
            ' Den zuvor gespeicherten Zustand wiederherstellen, der dieser Seite zugeordnet ist
            If e.PageState.ContainsKey("DisplayModeCategories") Then
                DisplayModeCategories = DirectCast(e.PageState("DisplayModeCategories"), Boolean)
            End If
        End If

        If DisplayModeCategories Then
            CategoryMode_Click(Nothing, Nothing)
        Else
            CriteriaMode_Click(Nothing, Nothing)
        End If

        If App.SearchBoxIsSupported Then
            Try
                Dim searchSuggestions = New Windows.ApplicationModel.Search.LocalContentSuggestionSettings()
                searchSuggestions.Enabled = True
                Dim rootFolder = Await categories.GetStorageFolderAsync("")
                searchSuggestions.Locations.Add(rootFolder)
                RecipeSearchBox.SetLocalContentSuggestionSettings(searchSuggestions)
                LeftRecipeSearchBox.SetLocalContentSuggestionSettings(searchSuggestions)
            Catch ex As Exception
            End Try

        End If

        itemGridView.SelectedItem = Nothing


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
        e.PageState("DisplayModeCategories") = DisplayModeCategories

    End Sub

    Private Sub ToggleSplitView_Click(sender As Object, e As RoutedEventArgs) Handles ToggleSplitView.Click
        RootSplitView.IsPaneOpen = Not RootSplitView.IsPaneOpen
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
        Dim rootFrame As Frame = Window.Current.Content

        rootFrame.BackStack.Clear()
        Common.SuspensionManager.ResetSessionState()
        currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed

        _navigationHelper.OnNavigatedTo(e)
    End Sub


    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        _navigationHelper.OnNavigatedFrom(e)
    End Sub

#End Region

#Region "VisualStates"
    Private Sub RenderSearchElements(state As VisualState)
        If state.Equals(SmallPhones) Then
            LeftRecipeAutoSuggestBox.SetValue(MarginProperty, New Thickness(0, 20, 90, 0))
            LeftRecipeSearchBox.SetValue(MarginProperty, New Thickness(0, 20, 90, 20))
            LeftRecipeAutoSuggestBox.Width = 250
            LeftRecipeSearchBox.Width = 250
            LeftEditMode.Visibility = Visibility.Visible
            If LeftEditMode.IsChecked Then
                LeftRecipeAutoSuggestBox.Visibility = Visibility.Collapsed
                LeftRecipeSearchBox.Visibility = Visibility.Collapsed
            Else
                LeftRecipeAutoSuggestBox.Visibility = App.AutoSuggestBoxVisibility
                LeftRecipeSearchBox.Visibility = App.SearchBoxVisibility
            End If
            EditMode.Visibility = Visibility.Collapsed
            RecipeAutoSuggestBox.Visibility = Visibility.Collapsed
            RecipeSearchBox.Visibility = Visibility.Collapsed
            SearchRequestButton.Visibility = Visibility.Collapsed
            pageTitle.Visibility = Visibility.Collapsed
        ElseIf state.Equals(Phones) Then
            LeftRecipeAutoSuggestBox.Visibility = Visibility.Collapsed
            LeftRecipeSearchBox.Visibility = Visibility.Visible
            LeftEditMode.Visibility = Visibility.Visible
            LeftRecipeAutoSuggestBox.SetValue(MarginProperty, New Thickness(0, 20, 40, 10))
            LeftRecipeSearchBox.SetValue(MarginProperty, New Thickness(0, 20, 40, 10))
            EditMode.Visibility = Visibility.Collapsed
            RecipeAutoSuggestBox.Visibility = Visibility.Collapsed
            RecipeSearchBox.Visibility = Visibility.Collapsed
            SearchRequestButton.Visibility = Visibility.Collapsed
            pageTitle.Visibility = Visibility.Collapsed
        ElseIf state.Equals(Tablet) Then
            LeftRecipeAutoSuggestBox.Visibility = Visibility.Collapsed
            LeftEditMode.Visibility = Visibility.Collapsed
            LeftRecipeSearchBox.Visibility = Visibility.Collapsed
            RecipeAutoSuggestBox.Visibility = Visibility.Collapsed
            RecipeSearchBox.Visibility = Visibility.Collapsed
            EditMode.Visibility = Visibility.Visible
            SearchRequestButton.Visibility = Visibility.Visible
            pageTitle.Visibility = Visibility.Visible
        Else
            LeftRecipeAutoSuggestBox.Visibility = Visibility.Collapsed
            LeftEditMode.Visibility = Visibility.Collapsed
            LeftRecipeSearchBox.Visibility = Visibility.Collapsed
            EditMode.Visibility = Visibility.Visible
            RecipeAutoSuggestBox.Visibility = App.AutoSuggestBoxVisibility
            RecipeSearchBox.Visibility = App.SearchBoxVisibility
            SearchRequestButton.Visibility = Visibility.Collapsed
            pageTitle.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub VisualStateGroup_CurrentStateChanged(sender As Object, e As VisualStateChangedEventArgs) Handles VisualStateGroup.CurrentStateChanged
        RenderSearchElements(e.NewState)
    End Sub
#End Region

#Region "Search"
    Private Sub SearchBox_QuerySubmitted(sender As SearchBox, args As SearchBoxQuerySubmittedEventArgs)


        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        categories.SearchResultsFolder.SetSearchParameter("", args.QueryText)
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), SearchResults.FolderName)

    End Sub

    Private Sub RecipeAutoSuggestBox_QuerySubmitted(sender As AutoSuggestBox, args As AutoSuggestBoxQuerySubmittedEventArgs) Handles RecipeAutoSuggestBox.QuerySubmitted

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        categories.SearchResultsFolder.SetSearchParameter("", args.QueryText)
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), SearchResults.FolderName)

    End Sub

    Private Sub SearchRequestButton_Click(sender As Object, e As RoutedEventArgs) Handles SearchRequestButton.Click

        SearchRequestButton.Visibility = Visibility.Collapsed
        pageTitle.Visibility = Visibility.Collapsed
        LeftRecipeSearchBox.Visibility = App.SearchBoxVisibility
        LeftRecipeAutoSuggestBox.Visibility = App.AutoSuggestBoxVisibility

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

#Region "ChangeRootFolder"

    Private Async Function ChangeRootFolder() As Task

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        Await categories.ChangeRootFolder()

        NavigationHelper_LoadState(Nothing, Nothing)

    End Function

#End Region

#Region "Edit Categories"
    Private Sub SetEditMode(isActive As Boolean)
        For Each f In Categories.Folders
            If f.Name <> RecipeTemplates.FolderName AndAlso Not f.IsPlus Then
                f.EditModeActive = isActive
            End If
        Next
        For Each t In Categories.TagFolders
            If Not t.IsPlus Then
                t.EditModeActive = isActive
            End If
        Next
    End Sub

    Private Sub HandleEditMode()
        LeftEditMode.IsChecked = EditMode.IsChecked
        SetEditMode(EditMode.IsChecked)
    End Sub

    Private Sub HandleLeftEditMode()
        EditMode.IsChecked = LeftEditMode.IsChecked
        SetEditMode(LeftEditMode.IsChecked)
    End Sub

    Private Cancelled As Boolean
    Private Async Sub DeleteTile_Click(sender As Object, e As RoutedEventArgs)
        Dim tile = Categories.GetFolder(DirectCast(DirectCast(sender, Button).CommandParameter, String))
        If tile Is Nothing Then
            Return
        End If

        Dim confirmText As String
        If DisplayModeCategories Then
            If Not tile.ContentLoaded Then
                Await tile.LoadAsync()
            End If
            If tile.Recipes.Count > 0 Or tile.SubCategories.Count > 0 Then
                Dim msg = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("CategoryNotEmpty"))
                Await msg.ShowAsync()
                Return
            End If
            confirmText = "DoYouWantToDeleteCategory"
        Else
            confirmText = "DoYouWantToDeleteTag"
        End If

        Cancelled = False
        Dim messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString(confirmText))

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

        If Not Cancelled Then
            Await RecipeFolders.Current.DeleteFolder(tile)
            If DisplayModeCategories Then
                If Categories.Folders.Count <= 2 Then ' Templates and plus
                    LeftEditMode.IsChecked = False
                    EditMode.IsChecked = False
                End If
            Else
                If Categories.TagFolders.Count <= 1 Then ' Plus
                    LeftEditMode.IsChecked = False
                    EditMode.IsChecked = False
                End If
            End If
        End If
    End Sub

    Private Async Function EditTileAsync(tile As RecipeFolder) As Task
        If tile IsNot Nothing Then
            Dim editor As DefineCategoryDialog
            If DisplayModeCategories Then
                editor = New DefineCategoryDialog(DefineCategoryDialog.ItemType.Category, tile.Name)
            Else
                editor = New DefineCategoryDialog(DefineCategoryDialog.ItemType.Tag, tile.Name, tile.Background, tile.Foreground)
            End If
            Await editor.ShowAsync()
        End If
    End Function

    Private Async Function EditTile_Click(sender As Object, e As RoutedEventArgs) As Task
        Dim tile = Categories.GetFolder(DirectCast(DirectCast(sender, Button).CommandParameter, String))
        Await EditTileAsync(tile)
    End Function

    Private Async Function NewTile_Click(sender As Object, e As RoutedEventArgs) As Task
        Dim editor As DefineCategoryDialog
        If DisplayModeCategories Then
            editor = New DefineCategoryDialog(DefineCategoryDialog.ItemType.Category, Background:=Colors.White, Foreground:=Colors.Black)
        Else
            editor = New DefineCategoryDialog(DefineCategoryDialog.ItemType.Tag)
        End If
        Await editor.ShowAsync()
    End Function

    Private Sub EditMode_Checked(sender As Object, e As RoutedEventArgs) Handles EditMode.Checked
        HandleEditMode()
    End Sub

    Private Sub LeftEditMode_Checked(sender As Object, e As RoutedEventArgs) Handles LeftEditMode.Checked, LeftEditMode.Unchecked
        HandleLeftEditMode()
    End Sub

    Private Async Function HandleEditCategory() As Task
        If itemGridView.SelectedItem Is Nothing Then
            Dim message As String
            If DisplayModeCategories Then
                message = App.Texts.GetString("SelectACategory")
            Else
                message = App.Texts.GetString("SelectATag")
            End If
            Dim msg = New Windows.UI.Popups.MessageDialog(message)
            Await msg.ShowAsync()
            Return
        End If

        Dim selectedCategory = DirectCast(itemGridView.SelectedItem, RecipeFolder)

        If selectedCategory.Name = RecipeTemplates.FolderName Then
            Dim msg = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("CannotEdit"))
            Await msg.ShowAsync()
            Return
        End If

        Dim editor As DefineCategoryDialog
        If DisplayModeCategories Then
            editor = New DefineCategoryDialog(DefineCategoryDialog.ItemType.Category, selectedCategory.Name)
        Else
            editor = New DefineCategoryDialog(DefineCategoryDialog.ItemType.Tag, selectedCategory.Name, selectedCategory.Background, selectedCategory.Foreground)
        End If
        Await editor.ShowAsync()
    End Function

#End Region

#Region "Navigation"

    Private Async Sub CategorySelected(sender As Object, e As ItemClickEventArgs) Handles itemGridView.ItemClick
        If e.ClickedItem IsNot Nothing Then
            Dim folders = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
            Dim category = (DirectCast(e.ClickedItem, RecipeFolder))
            If folders IsNot Nothing Then
                RootSplitView.IsPaneOpen = False
                If category.IsPlus Then
                    Await NewTile_Click(Nothing, Nothing)
                ElseIf EditMode.IsChecked Then
                    Await EditTileAsync(category)
                Else
                    Me.Frame.Navigate(GetType(RecipesPage), category.Name)
                End If
            End If
        End If
    End Sub

    Private Sub ShowFavorites_Click(sender As Object, e As RoutedEventArgs)
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), Favorites.FolderName)
    End Sub

    Private Sub FavoriteRecipesText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles FavoriteRecipesText.Tapped
        RootSplitView.IsPaneOpen = False
        Me.Frame.Navigate(GetType(RecipesPage), Favorites.FolderName)
    End Sub

#End Region

#Region "Timers"
    Private Sub ShowTimers_Click(sender As Object, e As RoutedEventArgs) Handles ShowTimers.Click
        Dim newSetting As Boolean = Not TimerController.TimersPaneOpen
        TimerController.TimersPaneOpen = newSetting
    End Sub

    Private Sub ShowTimersText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles ShowTimersText.Tapped
        Dim newSetting As Boolean = Not TimerController.TimersPaneOpen
        TimerController.TimersPaneOpen = newSetting
    End Sub

#End Region

#Region "History"
    Private Sub ShowHistory_Click(sender As Object, e As RoutedEventArgs) Handles ShowHistory.Click
        Me.Frame.Navigate(GetType(RecipesPage), History.FolderName)
    End Sub

    Private Sub ShowHistoryText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles ShowHistoryText.Tapped
        Me.Frame.Navigate(GetType(RecipesPage), History.FolderName)
    End Sub

#End Region

#Region "AppSettings"
    Public ChangeRootFolderRequested As Boolean

    Private Async Function HandleAppSettings() As Task
        ChangeRootFolderRequested = False
        Dim settingsDialog = New SettingsDialog
        Await settingsDialog.ShowAsync()
        If ChangeRootFolderRequested Then
            Await ChangeRootFolder()
        End If
    End Function

    Private Async Sub AppSettings_Click(sender As Object, e As RoutedEventArgs) Handles AppSettings.Click
        Await HandleAppSettings()
    End Sub

    Private Async Sub SettingsText_TappedAsync(sender As Object, e As TappedRoutedEventArgs) Handles SettingsText.Tapped
        Await HandleAppSettings()
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

#Region "ModeSwitch"
    Private Sub CategoryMode_Click(sender As Object, e As RoutedEventArgs)
        DisplayModeCategories = True
        itemGridView.ItemsSource = Categories.Folders
        CategoryModePanel.Visibility = Visibility.Collapsed
        CriteriaModePanel.Visibility = Visibility.Visible
        pageTitle.Text = App.Texts.GetString("CategoriesPageTitle")
    End Sub

    Private Sub CriteriaMode_Click(sender As Object, e As RoutedEventArgs)
        DisplayModeCategories = False
        itemGridView.ItemsSource = Categories.TagFolders
        CategoryModePanel.Visibility = Visibility.Visible
        CriteriaModePanel.Visibility = Visibility.Collapsed
        pageTitle.Text = App.Texts.GetString("CriteriaPageTitle")
    End Sub

    Private Sub CategoryModeText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles CategoryModeText.Tapped
        CategoryMode_Click(sender, Nothing)
    End Sub

    Private Sub CriteriaModeText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles CriteriaModeText.Tapped
        CriteriaMode_Click(sender, Nothing)
    End Sub
#End Region

#Region "ExportImport"
    Private Async Sub ExportHistory_Click(sender As Object, e As RoutedEventArgs)
        Await History.Current.ExportHistoryAsync()
    End Sub

    Private Async Sub ImportHistory_Click(sender As Object, e As RoutedEventArgs)
        Await History.Current.ImportHistoryAsync()
    End Sub

    Private Async Sub ExportDatabase_Click(sender As Object, e As RoutedEventArgs)
        Await MetaDataDatabase.Current.ExportMetadataAsync()
    End Sub

    Private Async Sub ImportDatabase_Click(sender As Object, e As RoutedEventArgs)
        Await MetaDataDatabase.Current.ImportMetadataAsync()
    End Sub

    Private Async Sub ScanForCalories_Click(sender As Object, e As RoutedEventArgs)
        Dim scanProgress = New ScanForCaloriesProgressDialog
        Await scanProgress.ShowAsync()
    End Sub
#End Region
End Class

