Imports SQLite
Imports Windows.Storage

Namespace Global.RecipeBrowser.Persistency

    Public Class CookingHistory

        Public Shared Current As CookingHistory

#Region "Properties"
        Private _CategoryFilter As String = String.Empty
        Property CategoryFilter As String
            Set(value As String)
                If Not CategoryFilter.Equals(value) Then
                    _CategoryFilter = value
                    _matchingEntriesCounted = False
                End If
            End Set
            Get
                Return _CategoryFilter
            End Get
        End Property


        Public Selection As New ObservableCollection(Of CookedRecipe)

        Public ReadOnly Property Count As Integer
            Get
                If Not _matchingEntriesCounted Then
                    Dim queryStr As String
                    Dim today As String = Persistency.Helper.GetSQLiteDate(Date.Now)
                    If Database Is Nothing Then
                        _matchingEntries = 0
                    Else
                        If CategoryFilter <> "eee" Then 'String.Empty Then
                            queryStr = "SELECT * FROM CookedRecipe WHERE LastCooked <= '" + today + "'"
                        Else
                            queryStr = "SELECT * FROM CookedRecipe WHERE LastCooked <= '" + today + "' AND Category = '" + CategoryFilter + "'"
                        End If
                        Dim query = Database.Query(Mapping, queryStr)
                        _matchingEntries = query.Count
                    End If
                    _matchingEntriesCounted = True
                End If
                Return _matchingEntries
            End Get
        End Property

#End Region

        Private Database As SQLite.SQLiteConnection
        Private Mapping As TableMapping
        Private StartDate As Date
        Private StartDateSet As Boolean

        Private _matchingEntries As Integer
        Private _matchingEntriesCounted As Boolean

#Region "Lifecycle"

        Private Async Function ExistsFile(folder As StorageFolder, filename As String) As Task(Of Boolean)
            Try
                Await folder.GetFileAsync(filename)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Sub OpenDatabase()
            Dim Path As String = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\history.db"

            Try
                If Database IsNot Nothing Then
                    If Database.DatabasePath.Equals(Path) Then
                        App.Logger.Write("CookingHistory.OpenDatabase: Database is already open.")
                        Return
                    Else
                        CloseDatabase()
                    End If
                End If

                Database = New SQLite.SQLiteConnection(Path)
                Database.CreateTable(GetType(CookedRecipe))
                Mapping = Database.GetMapping(GetType(CookedRecipe))
                Current = Me
            Catch ex As Exception
                Database = Nothing
            End Try
        End Sub

        Public Sub CloseDatabase()

            If Database IsNot Nothing Then
                Database.Close()
                Database = Nothing
                Current = Nothing
            End If

        End Sub

        Public Async Function TryRestoreBackupAsync(rootFolder As StorageFolder) As Task(Of Boolean)

            If rootFolder Is Nothing Then
                Return False
            End If

            CloseDatabase()

            Dim Path As String = ApplicationData.Current.LocalFolder.Path + "\history.db"

            Try
                Dim backup As StorageFile

                backup = Await rootFolder.GetFileAsync("history.db")
                Await backup.CopyAsync(ApplicationData.Current.LocalFolder, "history.db", NameCollisionOption.ReplaceExisting)
                Return True
            Catch ex As Exception
            End Try

            Return False

        End Function

        Public Async Function CreateBackupAndCloseAsync(rootFolder As StorageFolder) As Task

            If rootFolder Is Nothing Or Database Is Nothing Then
                Return
            End If

            CloseDatabase()

            Try
                Dim history As StorageFile

                history = Await ApplicationData.Current.LocalFolder.GetFileAsync("history.db")
                Await history.CopyAsync(rootFolder, "history.db", NameCollisionOption.ReplaceExisting)
            Catch ex As Exception
            End Try

        End Function

        Public Async Function CheckDatabaseExistsAsync() As Task(Of Boolean)
            Return Await ExistsFile(ApplicationData.Current.LocalFolder, "history.db")
        End Function

        Public Function IsAvailable() As Boolean
            Return Database IsNot Nothing
        End Function

#End Region

#Region "ExportImport"

        Public Async Function ExportHistory(exportFile As StorageFile) As Task(Of Boolean)

            If exportFile Is Nothing Or Database Is Nothing Then
                Return False
            End If

            CloseDatabase()

            Dim result As Boolean = True
            Try
                Dim history As StorageFile

                history = Await ApplicationData.Current.LocalFolder.GetFileAsync("history.db")
                Await history.CopyAndReplaceAsync(exportFile)
            Catch ex As Exception
                result = False
            End Try

            OpenDatabase()

            Return result
        End Function

        Public Async Function ImportHistory(importFile As StorageFile) As Task(Of Boolean)

            If importFile Is Nothing Then
                Return False
            End If

            If Database IsNot Nothing Then
                CloseDatabase()
            End If

            Dim result As Boolean = True
            Try
                Dim history As StorageFile

                history = Await ApplicationData.Current.LocalFolder.CreateFileAsync("history.db", CreationCollisionOption.ReplaceExisting)
                Await importFile.CopyAndReplaceAsync(history)
            Catch ex As Exception
                result = False
            End Try

            OpenDatabase()

            Return result
        End Function


#End Region

#Region "Write Access"
        Public Sub DeleteAll()
            If Database Is Nothing Then
                Return
            End If

            Database.DeleteAll(Of CookedRecipe)()
            Database.Commit()
            _matchingEntriesCounted = False
        End Sub

        Public Sub LogRecipeCooked(ByRef aCategory As String, ByRef aTitle As String, ByRef aDate As Date, Optional aSource As String = "")
            If Database Is Nothing Then
                Return
            End If

            If Not RecipeWasCookedOn(aCategory, aTitle, aDate) Then
                Database.Insert(New CookedRecipe(aCategory, aTitle, aDate, aSource))
                Database.Commit()
                RecipeBrowser.History.Current.Invalidate()
                _matchingEntriesCounted = False
            End If
        End Sub

        Public Sub MergeHistory(importFile As StorageFile, ByRef addedRecipes As Integer)
            addedRecipes = 0

            If importFile Is Nothing Then
                Return
            End If

            If Database Is Nothing Then
                Return
            End If

            Dim history As TableMapping

            Dim toImport = OpenADatabase(importFile.Path, history)

            If toImport Is Nothing OrElse history Is Nothing Then
                Return
            End If

            Dim cooked = GetHistory(toImport, history)

            For Each c In cooked
                If Not RecipeWasCookedOn(c.Category, c.Title, c.LastCooked) Then
                    LogRecipeCooked(c.Category, c.Title, c.LastCooked)
                    addedRecipes += 1
                End If
            Next

            toImport.Close()
        End Sub

        Public Function GetStatistics(ByRef currentDbHistoryEntries As Integer) As Boolean
            currentDbHistoryEntries = 0
            If Database Is Nothing Then
                Return False
            End If
            currentDbHistoryEntries = GetHistory(Database, Mapping).Count
            Return True
        End Function

        Public Function GetStatistics(importFile As StorageFile, ByRef currentDbHistoryEntries As Integer) As Boolean
            currentDbHistoryEntries = 0
            If Database Is Nothing Then
                Return False
            End If

            Dim history As TableMapping

            Dim toImport = OpenADatabase(importFile.Path, history)

            If toImport Is Nothing OrElse history Is Nothing Then
                Return False
            End If

            currentDbHistoryEntries = GetHistory(toImport, history).Count
            Return True
        End Function

        Private Function OpenADatabase(path As String, ByRef history As TableMapping) As SQLiteConnection
            Dim db As New SQLiteConnection(path)
            Try
                db.CreateTable(GetType(CookedRecipe))
                history = db.GetMapping(GetType(CookedRecipe))
            Catch ex As Exception
                db = Nothing
            End Try
            Return db
        End Function

        Public Sub ChangeCategory(Title As String, OldCategory As String, NewCategory As String)
            Dim queryStr As String
            queryStr = "UPDATE CookedRecipe SET Category = '" + EscapeSpecialCharacters(NewCategory) +
                       "' WHERE Category = '" + EscapeSpecialCharacters(OldCategory) +
                       "'  AND Title = '" + EscapeSpecialCharacters(Title) + "'"
            Dim query = Database.Query(Mapping, queryStr)
            Database.Commit()
        End Sub

        Public Sub RenameCategory(OldCategory As String, NewCategory As String)
            Dim queryStr As String
            queryStr = "UPDATE CookedRecipe SET Category = '" + EscapeSpecialCharacters(NewCategory) +
                       "' WHERE Category = '" + EscapeSpecialCharacters(OldCategory) + "'"
            Dim query = Database.Query(Mapping, queryStr)
            Database.Commit()
        End Sub

        Friend Sub RenameRecipe(oldName As String, category As String, newName As String)
            Dim queryStr As String
            queryStr = "UPDATE CookedRecipe SET Title = '" + EscapeSpecialCharacters(newName) +
                       "' WHERE Category = '" + EscapeSpecialCharacters(category) +
                       "'  AND Title = '" + EscapeSpecialCharacters(oldName) + "'"
            Dim query = Database.Query(Mapping, queryStr)
            Database.Commit()
        End Sub

        Private Function EscapeSpecialCharacters(ByRef source As String) As String
            Return source.Replace("'", "''")
        End Function

#End Region

#Region "Read Access"
        Public Sub RefreshSelection()
            If StartDateSet Then
                SelectWithFilter(StartDate)
            Else
                SelectAll()
            End If
        End Sub

        Public Sub SelectAll(Optional onlyExternalRecipes As Boolean = False)
            Try
                Dim query = Database.Table(Of CookedRecipe)().OrderByDescending(Function(aRecipe) aRecipe.LastCooked)

                Selection.Clear()

                For Each c In query
                    If Not onlyExternalRecipes OrElse c.ExternalSource.Length > 0 Then
                        Selection.Add(c)
                    End If
                Next

            Catch ex As Exception
            End Try
        End Sub

        Public Sub SelectRecipe(Category As String, Title As String)
            Dim queryStr As String
            queryStr = "SELECT * FROM CookedRecipe WHERE Category = '" + EscapeSpecialCharacters(Category) +
                       "' AND Title = '" + EscapeSpecialCharacters(Title) + "'"
            Dim query = Database.Query(Mapping, queryStr).OrderByDescending(Function(aRecipe) aRecipe.LastCooked)

            Selection.Clear()
            For Each c In query
                Selection.Add(c)
            Next
        End Sub

        Public Function RecipeWasCookedOn(Category As String, Title As String, TheDate As Date) As Boolean
            Dim queryStr As String
            Dim CookedOn As String = Persistency.Helper.GetSQLiteDate(TheDate)
            queryStr = "SELECT * FROM CookedRecipe WHERE Category = '" + EscapeSpecialCharacters(Category) +
                       "' AND Title = '" + EscapeSpecialCharacters(Title) + "' AND LastCooked = '" + CookedOn + "'"
            'Return Database.Query(Mapping, queryStr).Count > 0
            Dim query = Database.Query(Mapping, queryStr)
            Return query.Count > 0
        End Function

        Public Sub SelectWithFilter(aStartDate As Date)
            Dim filterDate As String = Persistency.Helper.GetSQLiteDate(aStartDate)

            Dim queryStr As String
            If CategoryFilter = String.Empty Then
                queryStr = "SELECT * FROM CookedRecipe WHERE LastCooked > '" + filterDate + "'"
            Else
                queryStr = "SELECT * FROM CookedRecipe WHERE LastCooked > '" + filterDate +
                           "' AND Category = '" + EscapeSpecialCharacters(CategoryFilter) + "'"
            End If
            Dim query = Database.Query(Mapping, queryStr).OrderByDescending(Function(aRecipe) aRecipe.LastCooked)

            StartDate = aStartDate
            StartDateSet = True

            Selection.Clear()
            For Each c In query
                Selection.Add(c)
            Next
        End Sub

        Public Function AddWithInterval(aStartDate As Date, anEndDate As Date) As Integer
            Dim startDate As String = Persistency.Helper.GetSQLiteDate(aStartDate)
            Dim endDate As String = Persistency.Helper.GetSQLiteDate(anEndDate)

            Dim queryStr As String
            If CategoryFilter = String.Empty Then
                queryStr = "SELECT * FROM CookedRecipe WHERE LastCooked > '" + startDate + "' AND LastCooked <= '" + endDate + "'"
            Else
                queryStr = "SELECT * FROM CookedRecipe WHERE LastCooked > '" + startDate + "' AND LastCooked <= '" + endDate +
                           "' AND Category = '" + EscapeSpecialCharacters(CategoryFilter) + "'"
            End If
            Dim query = Database.Query(Mapping, queryStr).OrderByDescending(Function(aRecipe) aRecipe.LastCooked)

            startDate = aStartDate
            StartDateSet = True

            For Each c In query
                Selection.Add(c)
            Next

            Return query.Count
        End Function

        Public Function GetHistory(db As SQLiteConnection, history As TableMapping) As List(Of CookedRecipe)
            Dim result = New List(Of CookedRecipe)
            Dim queryStr As String
            queryStr = "SELECT * FROM CookedRecipe ORDER BY Category,Title"
            Dim query = db.Query(history, queryStr)
            For Each e In query
                result.Add(DirectCast(e, CookedRecipe))
            Next
            Return result
        End Function

#End Region





        'Public Function GetMinMatchingDate() As Date

        '    Dim theMinDate As Long
        '    Try
        '        theMinDate = Aggregate r In Database.Table(Of CookedRecipe)()
        '                         Into MinDate = Min(Persistency.Helper.GetDate(r.LastCooked).Ticks)

        '    Catch ex As Exception

        '    End Try
        '    Dim resultDate = Date.MinValue.AddTicks(theMinDate)

        '    Dim queryStr As String
        '    If CategoryFilter = String.Empty Then
        '        queryStr = "SELECT MIN(LastCooked) FROM CookedRecipe"
        '    Else
        '        queryStr = "SELECT MIN(LastCooked) FROM CookedRecipe WHERE Category = '" + CategoryFilter + "'"
        '    End If
        '    Dim query = Database.Query(Mapping, queryStr)
        '    If query.Count = 0 Then
        '        Return Date.MaxValue
        '    Else
        '        Dim x = query(0)
        '        Dim result As CookedRecipe = query(0)
        '        Return Persistency.Helper.GetDate(result.LastCooked)
        '    End If
        'End Function



    End Class

End Namespace
