Imports SQLite
Imports Windows.Storage

Namespace Global.RecipeBrowser.Persistency

    Public Class MetaDataAccess

        Public Shared Current As MetaDataAccess

        Private Database As SQLiteConnection
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

        Private Function OpenADatabase(path As String, ByRef recipeMetadata As TableMapping, ByRef tagDirectories As TableMapping, ByRef recipeTags As TableMapping) As SQLiteConnection
            Dim db As New SQLite.SQLiteConnection(path)
            Try
                db.CreateTable(GetType(RecipeMetaData))
                db.CreateTable(GetType(TagDirectory))
                db.CreateTable(GetType(RecipeTag))
                recipeMetadata = db.GetMapping(GetType(RecipeMetaData))
                tagDirectories = db.GetMapping(GetType(TagDirectory))
                recipeTags = db.GetMapping(GetType(RecipeTag))
            Catch ex As Exception
                db = Nothing
            End Try
            Return db
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

                Database = OpenADatabase(Path, RecipeMetadataTable, TagDirectoryTable, RecipeTagTable)
                If Database IsNot Nothing Then
                    Current = Me
                End If
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

        Public Function GetMetadata(Category As String, Title As String, Optional allowNullValue As Boolean = False) As RecipeMetaData
            Dim queryStr As String
            queryStr = "SELECT * FROM RecipeMetadata WHERE Category = '" + EscapeSpecialCharacters(Category) +
                       "' AND Title = '" + EscapeSpecialCharacters(Title) + "'"
            Dim query As RecipeMetaData = Database.Query(RecipeMetadataTable, queryStr).FirstOrDefault()
            If query Is Nothing Then
                Return If(allowNullValue, Nothing, New RecipeMetaData(Category, Title, RecipeBrowser.Recipe.CaloriesNotScanned))
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
            Return GetTagDirectory(Database, TagDirectoryTable)
        End Function

        Public Function GetTagDirectory(db As SQLite.SQLiteConnection, tagDirectories As TableMapping) As List(Of TagDirectory)
            Dim result = New List(Of TagDirectory)
            Dim queryStr As String
            queryStr = "SELECT * FROM TagDirectory ORDER BY Tag"
            Dim query = db.Query(tagDirectories, queryStr)
            For Each e In query
                result.Add(DirectCast(e, TagDirectory))
            Next
            Return result
        End Function

        Public Function GetRecipeMetadata(db As SQLite.SQLiteConnection, recipeMetadata As TableMapping) As List(Of RecipeMetaData)
            Dim result = New List(Of RecipeMetaData)
            Dim queryStr As String
            queryStr = "SELECT * FROM RecipeMetadata ORDER BY Category,Title"
            Dim query = db.Query(recipeMetadata, queryStr)
            For Each e In query
                result.Add(DirectCast(e, RecipeMetaData))
            Next
            Return result
        End Function

        Public Function GetRecipeTags(db As SQLite.SQLiteConnection, recipeTags As TableMapping) As List(Of RecipeTag)
            Dim result = New List(Of RecipeTag)
            Dim queryStr As String
            queryStr = "SELECT * FROM RecipeTag ORDER BY Category,Title"
            Dim query = db.Query(recipeTags, queryStr)
            For Each e In query
                result.Add(DirectCast(e, RecipeTag))
            Next
            Return result
        End Function

#End Region

#Region "ExportImport"

        Public Async Function ExportMetadata(exportFile As StorageFile) As Task(Of Boolean)

            If exportFile Is Nothing Or Database Is Nothing Then
                Return False
            End If

            CloseDatabase()

            Dim result As Boolean = True
            Try
                Dim metadata As StorageFile

                metadata = Await ApplicationData.Current.LocalFolder.GetFileAsync("metadata.db")
                Await metadata.CopyAndReplaceAsync(exportFile)
            Catch ex As Exception
                result = False
            End Try

            OpenDatabase()

            Return result
        End Function

        Public Async Function ImportMetadata(importFile As StorageFile) As Task(Of Boolean)

            If importFile Is Nothing Then
                Return False
            End If

            If Database IsNot Nothing Then
                CloseDatabase()
            End If

            Dim result As Boolean = True
            Try
                Dim metadata As StorageFile

                metadata = Await ApplicationData.Current.LocalFolder.CreateFileAsync("metadata.db", CreationCollisionOption.ReplaceExisting)
                Await importFile.CopyAndReplaceAsync(metadata)
            Catch ex As Exception
                result = False
            End Try

            OpenDatabase()

            Return result
        End Function

        Public Sub MergeMetadata(importFile As StorageFile, ByRef caloricInfoUpdates As Integer, ByRef recipeTagsUpdates As Integer, ByRef newTagDirectoryEntries As Integer)

            caloricInfoUpdates = 0
            recipeTagsUpdates = 0
            newTagDirectoryEntries = 0

            If importFile Is Nothing Then
                Return
            End If

            If Database Is Nothing Then
                Return
            End If

            Dim recipeMetadata As TableMapping
            Dim tagDirectory As TableMapping
            Dim recipeTags As TableMapping

            Dim toImport = OpenADatabase(importFile.Path, recipeMetadata, tagDirectory, recipeTags)

            If toImport Is Nothing OrElse recipeMetadata Is Nothing OrElse tagDirectory Is Nothing OrElse recipeTags Is Nothing Then
                Return
            End If

            Dim metadata = GetRecipeMetadata(toImport, recipeMetadata)
            Dim tags = GetRecipeTags(toImport, recipeTags)
            Dim directory = GetTagDirectory(toImport, tagDirectory)

            'Merge caloric infos
            For Each m In metadata
                Dim current = GetMetadata(m.Category, m.Title, allowNullValue:=True)
                If current IsNot Nothing Then
                    If m.Calories > 0 AndAlso m.Calories <> current.Calories Then
                        StoreCalories(m.Category, m.Title, m.Calories)
                        caloricInfoUpdates += 1
                    End If
                End If
            Next

            'Merge recipe tags
            Dim last As New RecipeTag
            Dim lastTags As New List(Of RecipeTag)
            For Each t In tags
                If t.Category.Equals(last.Category) AndAlso t.Title.Equals(last.Title) Then
                    lastTags.Add(t)
                Else
                    If MergeTags(last, lastTags) Then
                        recipeTagsUpdates += 1
                    End If
                    last = t
                    lastTags.Clear()
                    lastTags.Add(t)
                End If
            Next
            ' last item
            If MergeTags(last, lastTags) Then
                recipeTagsUpdates += 1
            End If

            'Merge tag directory
            Dim currentDir = GetTagDirectory()
            For Each t In directory
                'Dim dummy = currentDir.Find(Function(other) other.Tag.Equals(t.Tag))
                'dummy = currentDir.Find(Function(other) other.Tag.Equals("BBQ"))
                If currentDir.Find(Function(other) other.Tag.Equals(t.Tag)) Is Nothing Then
                    AddTag(t)
                    newTagDirectoryEntries += 1
                End If
            Next

            toImport.Close()
        End Sub

        Private Function MergeTags(last As RecipeTag, lastTags As List(Of RecipeTag)) As Boolean
            Dim update As Boolean = False
            If lastTags.Count > 0 Then
                Dim currentTags = GetTags(last.Category, last.Title)
                Dim updatedTags As New List(Of String)
                For Each l In lastTags
                    If currentTags.Find(Function(other) other.Tag.Equals(l.Tag)) Is Nothing Then
                        update = True
                    End If
                    updatedTags.Add(l.Tag)
                Next
                If update Then
                    StoreTags(last.Category, last.Title, updatedTags)
                End If
            End If
            Return update
        End Function

        Public Function GetStatistics(ByRef caloricInfos As Integer, ByRef recipeTags As Integer, ByRef tagDirectoryEntries As Integer) As Boolean

            caloricInfos = 0
            recipeTags = 0
            tagDirectoryEntries = 0

            If Database Is Nothing Then
                Return False
            End If

            caloricInfos = GetRecipeMetadata(Database, RecipeMetadataTable).Count
            recipeTags = GetRecipeTags(Database, RecipeTagTable).Count
            tagDirectoryEntries = GetTagDirectory().Count

            Return True
        End Function

        Public Function GetStatistics(importFile As StorageFile, ByRef caloricInfos As Integer, ByRef recipeTags As Integer, ByRef tagDirectoryEntries As Integer) As Boolean

            caloricInfos = 0
            recipeTags = 0
            tagDirectoryEntries = 0

            If Database Is Nothing Then
                Return False
            End If

            Dim recipeMetadata As TableMapping
            Dim tagDirectory As TableMapping
            Dim recipeTagsMapping As TableMapping

            Dim toImport = OpenADatabase(importFile.Path, recipeMetadata, tagDirectory, recipeTagsMapping)

            If toImport Is Nothing OrElse recipeMetadata Is Nothing OrElse tagDirectory Is Nothing OrElse recipeTagsMapping Is Nothing Then
                Return false
            End If


            caloricInfos = GetRecipeMetadata(toImport, recipeMetadata).Count
            recipeTags = GetRecipeTags(toImport, recipeTagsMapping).Count
            tagDirectoryEntries = GetTagDirectory(toImport, tagDirectory).Count

            Return True
        End Function

#End Region

    End Class

End Namespace
