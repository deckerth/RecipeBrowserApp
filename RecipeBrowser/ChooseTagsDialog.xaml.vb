' Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Imports RecipeBrowser.ViewModels

Public NotInheritable Class ChooseTagsDialog
    Inherits ContentDialog

    Public Property TaggedRecipe As Recipe
    Public Property VisibleTags As New ObservableCollection(Of RecipeTagViewModel)
    Public Property CreateTagRequested As Boolean = False

    Private AllTags As New ObservableCollection(Of RecipeTagViewModel)

    Public Sub SetRecipe(aRecipe As Recipe)
        TaggedRecipe = aRecipe

        For Each t In TagRepository.Current.Directory
            Dim n = New RecipeTagViewModel(aRecipe, aTag:=t.Tag)
            AllTags.Add(n)
            VisibleTags.Add(n)
        Next
        TagListView.ItemsSource = VisibleTags
        Dim index As Integer = 0
        For Each i In VisibleTags
            If aRecipe.HasTag(i.Tag) Then
                TagListView.SelectRange(New ItemIndexRange(index, 1))
            End If
            index = index + 1
        Next
        CreateTagRequested = False
        If TagSearchPattern.Text.Length > 0 Then
            TagSearchPattern_TextChanged(Nothing, Nothing)
        End If
    End Sub

    Private Sub ContentDialog_PrimaryButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)
        Dim SelectedTags As New List(Of RecipeTagViewModel)
        For Each i In TagListView.SelectedItems
            SelectedTags.Add(i)
        Next
        TaggedRecipe.SetTags(SelectedTags)
    End Sub

    Private Sub ContentDialog_SecondaryButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)
    End Sub

    Private Sub TagSearchPattern_TextChanged(sender As AutoSuggestBox, args As AutoSuggestBoxTextChangedEventArgs) Handles TagSearchPattern.TextChanged
        Dim pattern = TagSearchPattern.Text.Trim().ToUpper()
        Dim visTagsIndex As Integer = 0

        ' Update procedure
        '
        ' AllTags    VisibleTags   =>  VisibleTags
        '
        '  a - add        a                a
        '  b              b
        '  c - add                         c
        '  d              d

        For Each t In AllTags
            Dim add As Boolean
            If pattern.Length = 0 Then
                add = True
            ElseIf t.Tag.ToUpper.Contains(pattern) Then
                add = True
            Else
                add = False
            End If

            If add Then
                If visTagsIndex = VisibleTags.Count OrElse Not VisibleTags(visTagsIndex).Equals(t) Then
                    VisibleTags.Insert(visTagsIndex, t)
                    If TaggedRecipe.HasTag(t.Tag) Then
                        TagListView.SelectRange(New ItemIndexRange(visTagsIndex, 1))
                    End If
                End If
                visTagsIndex = visTagsIndex + 1
            Else
                    If visTagsIndex < VisibleTags.Count AndAlso VisibleTags(visTagsIndex).Equals(t) Then
                    VisibleTags.RemoveAt(visTagsIndex)
                End If
            End If
        Next
    End Sub

    Private Sub CreateNewTag_Click(sender As Object, e As RoutedEventArgs) Handles CreateNewTag.Click
        CreateTagRequested = True
        ContentDialog_PrimaryButtonClick(Nothing, Nothing)
        Hide()
    End Sub
End Class
