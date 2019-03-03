Imports SQLite
Imports Windows.Storage

Namespace Global.RecipeBrowser.Persistency

    Public Class MetaDataAccess

        Public Shared Current As MetaDataAccess

        Private Database As SQLite.SQLiteConnection
        Private RecipeMetadataTable As TableMapping
        Private TagDirectoryTable As TableMapping
        Private RecipeTagTable As TableMapping

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
            Dim Path As String = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\metadata.db"

            Try
                If Database IsNot Nothing Then
                    If Database.DatabasePath.Equals(Path) Then
                        App.Logger.Write("MetadataAccess.OpenDatabase: Database is already open.")
                        Return
                    Else
                        CloseDatabase()
                    End If
                End If

                Database = New SQLite.SQLiteConnection(Path)
                Database.CreateTable(GetType(RecipeMetaData))
                Database.CreateTable(GetType(TagDirectory))
                Database.CreateTable(GetType(RecipeTag))
                RecipeMetadataTable = Database.GetMapping(GetType(RecipeMetaData))
                TagDirectoryTable = Database.GetMapping(GetType(TagDirectory))
                RecipeTagTable = Database.GetMapping(GetType(RecipeTag))
                Current = Me
                App.Logger.Write("MetadataAccess.OpenDatabase: Database is bound = " + (Database IsNot Nothing).ToString)
            Catch ex As Exception
                Database = Nothing
                App.Logger.WriteException("MetadataAccess.OpenDatabase: Database cannnot be opened.", ex)
            End Try
        End Sub

        Public Sub CloseDatabase()

            If Database IsNot Nothing Then
                Database.Close()
                Database = Nothing
                Current = Nothing
            End If

            App.Logger.Write("MetadataAccess.CloseDatabase: Database is bound = " + (Database IsNot Nothing).ToString)
            App.Logger.WriteStack()

        End Sub

        Public Async Function TryRestoreBackupAsync(rootFolder As StorageFolder) As Task(Of Boolean)

            If rootFolder Is Nothing Then
                Return False
            End If

            CloseDatabase()

            Dim Path As String = ApplicationData.Current.LocalFolder.Path + "\metadata.db"

            Try
                Dim backup As StorageFile

                backup = Await rootFolder.GetFileAsync("metadata.db")
                Await backup.CopyAsync(ApplicationData.Current.LocalFolder, "metadata.db", NameCollisionOption.ReplaceExisting)
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
                Dim metadata As StorageFile

                metadata = Await ApplicationData.Current.LocalFolder.GetFileAsync("metadata.db")
                Await metadata.CopyAsync(rootFolder, "metadata.db", NameCollisionOption.ReplaceExisting)
            Catch ex As Exception
            End Try

        End Function

        Public Async Function CheckDatabaseExistsAsync() As Task(Of Boolean)
            Return Await ExistsFile(ApplicationData.Current.LocalFolder, "metadata.db")
        End Function

        Public Function IsAvailable() As Boolean
            Return Database IsNot Nothing
        End Function
#End Region

#Region "Write Access"
        Public Sub StoreCalories(ByRef aCategory As String, ByRef aTitle As String, kcal As Integer)
            If Database Is Nothing Then
                Return
            End If
            Try
                Dim newInfo = New RecipeMetaData(aCategory, aTitle, kcal)
                Dim queryStr As String
                queryStr = "SELECT * FROM RecipeMetadata WHERE Category = '" + EscapeSpecialCharacters(aCategory) +
                       "' AND Title = '" + EscapeSpecialCharacters(aTitle) + "'"
                Dim query As RecipeMetaData = Database.Query(RecipeMetadataTable, queryStr).FirstOrDefault()
                If query IsNot Nothing Then
                    Database.Delete(query)
                End If
                Database.Insert(newInfo)
                Database.Commit()
            Catch ex As Exception
            End Try
        End Sub

        Public Sub StoreTags(ByRef aCategory As String, ByRef aTitle As String, tags As List(Of String))
            If Database Is Nothing Then
                Return
            End If
            Try
                Dim oldTags As List(Of RecipeTag) = GetTags(aCategory, aTitle)
                For Each t In oldTags
                    Database.Delete(t)
                Next
                For Each t In tags
                    Database.Insert(New RecipeTag(aCategory, aTitle, t))
                Next
                Database.Commit()
            Catch ex As Exception
            End Try
        End Sub

        Public Sub AddTag(newTag As TagDirectory)
            Dim queryStr As String
            queryStr = "SELECT * FROM TagDirectory WHERE Tag = '" + EscapeSpecialCharacters(newTag.Tag) + "'"
            Dim query As TagDirectory = Database.Query(TagDirectoryTable, queryStr).FirstOrDefault()
            If query Is Nothing Then
                Database.Insert(newTag)
                Database.Commit()
            End If
        End Sub

        Public Sub DeleteTag(toDelete As String)
            Dim queryStr As String
            queryStr = "SELECT * FROM TagDirectory WHERE Tag = '" + EscapeSpecialCharacters(toDelete) + "'"
            Dim query As TagDirectory = Database.Query(TagDirectoryTable, queryStr).FirstOrDefault()
            If query IsNot Nothing Then
                Database.Delete(query)

                Dim tagEntries = GetRecipesForTag(toDelete)
                For Each e In tagEntries
                    Database.Delete(e)
                Next

                Database.Commit()
            End If
        End Sub

        Public Sub RenameTag(oldTag As String, newTag As TagDirectory)

            Dim nameDiffers = Not oldTag.Equals(newTag.Tag)

            Dim queryStr As String
            queryStr = "SELECT * FROM TagDirectory WHERE Tag = '" + EscapeSpecialCharacters(oldTag) + "'"
            Dim query As TagDirectory = Database.Query(TagDirectoryTable, queryStr).FirstOrDefault()
            If query IsNot Nothing Then
                Database.Delete(query)
                Database.Insert(newTag)
                If nameDiffers Then
                    queryStr = "UPDATE RecipeTag SET Tag = '" + EscapeSpecialCharacters(newTag.Tag) +
                           "' WHERE Tag = '" + EscapeSpecialCharacters(oldTag) + "'"
                    Database.Query(RecipeTagTable, queryStr)
                End If

                Database.Commit()
            End If
        End Sub

        Public Sub ChangeCategory(Title As String, OldCategory As String, NewCategory As String)
            Dim queryStr As String
            queryStr = "UPDATE RecipeMetadata SET Category = '" + EscapeSpecialCharacters(NewCategory) +
                       "' WHERE Category = '" + EscapeSpecialCharacters(OldCategory) +
                       "'  AND Title = '" + EscapeSpecialCharacters(Title) + "'"
            Database.Query(RecipeMetadataTable, queryStr)

            queryStr = "UPDATE RecipeTag SET Category = '" + EscapeSpecialCharacters(NewCategory) +
                       "' WHERE Category = '" + EscapeSpecialCharacters(OldCategory) +
                       "'  AND Title = '" + EscapeSpecialCharacters(Title) + "'"
            Database.Query(RecipeTagTable, queryStr)

            Database.Commit()
        End Sub

        Public Sub RenameCategory(OldCategory As String, NewCategory As String)
            Dim queryStr As String
            queryStr = "UPDATE RecipeMetadata SET Category = '" + EscapeSpecialCharacters(NewCategory) +
                       "' WHERE Category = '" + EscapeSpecialCharacters(OldCategory) + "'"
            Database.Query(RecipeMetadataTable, queryStr)

            queryStr = "UPDATE RecipeTag SET Category = '" + EscapeSpecialCharacters(NewCategory) +
                       "' WHERE Category = '" + EscapeSpecialCharacters(OldCategory) + "'"
            Database.Query(RecipeTagTable, queryStr)

            Database.Commit()
        End Sub

        Friend Sub RenameRecipe(oldName As String, category As String, newName As String)
            Dim queryStr As String
            queryStr = "UPDATE RecipeMetadata SET Title = '" + EscapeSpecialCharacters(newName) +
                       "' WHERE Category = '" + EscapeSpecialCharacters(category) +
                       "'  AND Title = '" + EscapeSpecialCharacters(oldName) + "'"
            Database.Query(RecipeMetadataTable, queryStr)

            queryStr = "UPDATE RecipeTag SET Title = '" + EscapeSpecialCharacters(newName) +
                       "' WHERE Category = '" + EscapeSpecialCharacters(category) +
                       "'  AND Title = '" + EscapeSpecialCharacters(oldName) + "'"
            Database.Query(RecipeTagTable, queryStr)

            Database.Commit()
        End Sub

        Private Function EscapeSpecialCharacters(ByRef source As String) As String
            Return source.Replace("'", "''")
        End Function

#End Region

#Region "Read Access"

        Public Function GetMetadata(Category As String, Title As String) As RecipeMetadata
            Dim queryStr As String
            queryStr = "SELECT * FROM RecipeMetadata WHERE Category = '" + EscapeSpecialCharacters(Category) +
                       "' AND Title = '" + EscapeSpecialCharacters(Title) + "'"
            Dim query As RecipeMetadata = Database.Query(RecipeMetadataTable, queryStr).FirstOrDefault()
            If query Is Nothing Then
                Return New RecipeMetadata(Category, Title, RecipeBrowser.Recipe.CaloriesNotScanned)
            Else
                Return query
            End If
        End Function

        Public Function GetMetadataForCategory(Category As String) As RecipeMetaDataList
            Dim result = New RecipeMetaDataList

            If Database Is Nothing Then
                App.Logger.Write("MetadataAccess.GetMetadataForCategory: Database Is Nothing")
                Return result
            End If
            If RecipeMetadataTable Is Nothing Then
                App.Logger.Write("MetadataAccess.GetMetadataForCategory: RecipeMetadataTable Is Nothing")
                Return result
            End If

            Dim queryStr As String
            queryStr = "SELECT * FROM RecipeMetadata WHERE Category = '" + EscapeSpecialCharacters(Category) + "'"
            Dim query = Database.Query(RecipeMetadataTable, queryStr)
            For Each e In query
                Dim m As RecipeMetadata = e
                result.Entries.Add(m)
            Next
            Return result
        End Function

        Public Function GetTags(Category As String, Title As String) As List(Of RecipeTag)
            Dim result As New List(Of RecipeTag)
            If Database Is Nothing Then
                App.Logger.Write("MetadataAccess.GetTags: Database Is Nothing")
                Return result
            End If
            If RecipeTagTable Is Nothing Then
                App.Logger.Write("MetadataAccess.GetTags: RecipeTagTable Is Nothing")
                Return result
            End If

            Dim queryStr As String
            queryStr = "SELECT * FROM RecipeTag WHERE Category = '" + EscapeSpecialCharacters(Category) +
                       "' AND Title = '" + EscapeSpecialCharacters(Title) + "'"
            For Each t In Database.Query(RecipeTagTable, queryStr)
                result.Add(DirectCast(t, RecipeTag))
            Next
            Return result
        End Function

        Public Function GetRecipesForTag(Tag As String) As List(Of RecipeTag)
            Dim result = New List(Of RecipeTag)
            Dim queryStr As String
            queryStr = "SELECT * FROM RecipeTag WHERE Tag = '" + EscapeSpecialCharacters(Tag) + "'"
            Dim query = Database.Query(RecipeTagTable, queryStr)
            For Each e In query
                result.Add(DirectCast(e, RecipeTag))
            Next
            Return result
        End Function

        Public Function GetTagDirectory() As List(Of TagDirectory)
            Dim result = New List(Of TagDirectory)
            Dim queryStr As String
            queryStr = "SELECT * FROM TagDirectory ORDER BY Tag"
            Dim query = Database.Query(TagDirectoryTable, queryStr)
            For Each e In query
                result.Add(DirectCast(e, TagDirectory))
            Next
            Return result
        End Function

#End Region

#Region "ExportImport"

        Public Async Function ExportMetadata(exportFile As StorageFile) As Task

            If exportFile Is Nothing Or Database Is Nothing Then
                Return
            End If

            CloseDatabase()

            Try
                Dim metadata As StorageFile

                metadata = Await ApplicationData.Current.LocalFolder.GetFileAsync("metadata.db")
                Await metadata.CopyAndReplaceAsync(exportFile)
            Catch ex As Exception
            End Try

            OpenDatabase()

        End Function

        Public Async Function ImportMetadata(importFile As StorageFile) As Task

            If importFile Is Nothing Then
                Return
            End If

            If Database IsNot Nothing Then
                CloseDatabase()
            End If

            Try
                Dim metadata As StorageFile

                metadata = Await ApplicationData.Current.LocalFolder.CreateFileAsync("metadata.db", CreationCollisionOption.ReplaceExisting)
                Await importFile.CopyAndReplaceAsync(metadata)
            Catch ex As Exception
            End Try

            OpenDatabase()

        End Function


#End Region

    End Class

End Namespace
