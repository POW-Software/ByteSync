﻿using System.IO;
using ByteSync.Business.Inventories;
using ByteSync.Models.FileSystems;

namespace ByteSync.Services.Inventories;

public class InventoryIndexer
{
    internal InventoryIndexer()
    {
        IndexedItemsByFileDescription = new Dictionary<string, IndexedItem>();
        IndexedItemsByPathIdentity = new Dictionary<string, HashSet<IndexedItem>>();
    }

    private Dictionary<string, IndexedItem> IndexedItemsByFileDescription { get; }
    private Dictionary<string,  HashSet<IndexedItem>> IndexedItemsByPathIdentity { get; }

    internal void Register(FileDescription fileDescription, FileInfo fileInfo)
    {
        var indexedItem = new IndexedItem(fileDescription, fileInfo);

        var index = BuildFileDescriptionIndex(fileDescription);
        
        IndexedItemsByFileDescription.Add(index, indexedItem);
    }
    
    internal void Register(DirectoryDescription directoryDescription, DirectoryInfo directoryInfo)
    {
        var indexedItem = new IndexedItem(directoryDescription, directoryInfo);
    
        var index = BuildFileDescriptionIndex(directoryDescription);
        
        IndexedItemsByFileDescription.Add(index, indexedItem);
    }
    
    internal void Register(FileSystemDescription fileSystemDescription, PathIdentity pathIdentity)
    {
        var index = BuildFileDescriptionIndex(fileSystemDescription);

        if (IndexedItemsByFileDescription.ContainsKey(index))
        {
            var indexedItem = IndexedItemsByFileDescription[index];

            var pathIdentityIndex = BuildPathIdentityIndex(pathIdentity);

            if (!IndexedItemsByPathIdentity.ContainsKey(pathIdentityIndex))
            {
                IndexedItemsByPathIdentity.Add(pathIdentityIndex, new HashSet<IndexedItem>());
            }
            IndexedItemsByPathIdentity[pathIdentityIndex].Add(indexedItem);
        }
    }

    private string BuildFileDescriptionIndex(FileSystemDescription fileSystemDescription)
    {
        return $"{fileSystemDescription.InventoryPart.GetHashCode()}_{fileSystemDescription.FileSystemType}_{fileSystemDescription.RelativePath}";
    }
    
    private string BuildPathIdentityIndex(PathIdentity pathIdentity)
    {
        return $"{pathIdentity.GetHashCode()}";
    }
    
    internal HashSet<IndexedItem>? GetItemsBy(PathIdentity pathIdentity)
    {
        var pathIdentityIndex = BuildPathIdentityIndex(pathIdentity);

        if (IndexedItemsByPathIdentity.ContainsKey(pathIdentityIndex))
        {
            return IndexedItemsByPathIdentity[pathIdentityIndex];
        }
        else
        {
            return null;
        }
    }
}