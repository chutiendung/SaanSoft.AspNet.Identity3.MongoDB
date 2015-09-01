﻿using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Framework.Configuration;
using MongoDB.Driver;

namespace AspNet.Identity3.MongoDB.Tests
{
	public class DatabaseFixture : IDisposable
	{
		/// <param name="collectionPrefix">Unique prefix for the collection. Used so dropping collections during a test run wont interfere with other tests running in parallel. Suggest using the test class name/namespace.</param>
		/// <param name="databaseName">Defaults to "Testing"</param>
		/// <remarks>
		/// dropCollectionOnInit: Defaults to true
		/// dropCollectionOnDispose: Defaults to false
		/// </remarks>
		/// <example>
		/// collectionPrefix = $"{typeof([TEST CLASS]).Name}");
		/// </example>
		public DatabaseFixture(string collectionPrefix, string databaseName = null) : this(collectionPrefix, databaseName, true, false) { }


		/// <param name="collectionPrefix">Unique prefix for the collection. Used so dropping collections during a test run wont interfere with other tests running in parallel. Suggest using the test class name/namespace.</param>
		/// <param name="dropCollectionOnInit">Drops any collections in the database that start the collectionPrefix.</param>
		/// <param name="dropCollectionOnDispose">Drops any collections created during the test run and any collections in the database that start the collectionPrefix.</param>
		/// <remarks>
		/// databaseName: Defaults to "Testing"
		/// </remarks>
		/// <example>
		/// collectionPrefix = $"{typeof([TEST CLASS]).Name}");
		/// </example>
		public DatabaseFixture(string collectionPrefix, bool dropCollectionOnInit, bool dropCollectionOnDispose) : this(collectionPrefix, null, dropCollectionOnInit, dropCollectionOnDispose) { }


		/// <param name="collectionPrefix">Unique prefix for the collection. Used so dropping collections during a test run wont interfere with other tests running in parallel. Suggest using the test class name/namespace.</param>
		/// <param name="databaseName">Defaults to "Testing"</param>
		/// <param name="dropCollectionOnInit">Drops any collections in the database that start the collectionPrefix.</param>
		/// <param name="dropCollectionOnDispose">Drops any collections created during the test run and any collections in the database that start the collectionPrefix.</param>
		/// <example>
		/// collectionPrefix = $"{typeof([TEST CLASS]).Name}");
		/// </example>
		public DatabaseFixture(string collectionPrefix, string databaseName, bool dropCollectionOnInit, bool dropCollectionOnDispose)
		{
			RegisterClassMap<IdentityUser, IdentityRole, string>.Init();

			//Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
			//then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
			var builder = new ConfigurationBuilder(".\\").AddJsonFile("config.json");
			Configuration = builder.Build();

			ConnectionString = Configuration["Data:ConnectionString"];
			CollectionPrefix = string.IsNullOrWhiteSpace(collectionPrefix) ? DateTime.UtcNow.ToShortTimeString() : collectionPrefix;
			DatabaseName = !string.IsNullOrWhiteSpace(databaseName) ? databaseName
									: "Testing";

			DropCollectionOnInit = dropCollectionOnInit;
			DropCollectionOnDispose = dropCollectionOnDispose;

			if(DropCollectionOnInit) DropCollection();
		}


		public IConfiguration Configuration { get; protected set; }
		public string ConnectionString { get; protected set; }
		public string DatabaseName { get; protected set; }
		public string CollectionPrefix { get; protected set; }

		public IMongoClient MongoClient { get; protected set; }
		public IMongoDatabase MongoDatabase { get; protected set; }
		public bool DropCollectionOnInit { get; protected set; }
		public bool DropCollectionOnDispose { get; protected set; }

		public virtual void Dispose()
		{
			if(DropCollectionOnDispose) DropCollection();
		}

		public virtual void DropCollection()
		{
			foreach(var c in _mongoCollectionNames)
			{
				GetMongoDatabase().DropCollectionAsync(c).Wait();
			}
			var cursorTask = GetMongoDatabase().ListCollectionsAsync();
			cursorTask.Wait();

			var cursor = cursorTask.Result;
			cursor.ForEachAsync(c =>
			{
				var collectionName = c["name"].ToString();
				if (collectionName.StartsWith(CollectionPrefix))
				{
					GetMongoDatabase().DropCollectionAsync(collectionName).Wait();
				}
			});
		}

		public virtual IMongoClient GetMongoClient()
		{
			if(MongoClient == null)
			{
				MongoClient = new MongoClient(ConnectionString);
			}
			return MongoClient;
		}
		public virtual IMongoDatabase GetMongoDatabase()
		{
			if(MongoDatabase == null)
			{
				MongoDatabase = GetMongoClient().GetDatabase(DatabaseName);
			}
			return MongoDatabase;
		}

		public virtual IMongoCollection<T> GetCollection<T>()
		{
			var collectionName = $"{CollectionPrefix}_{typeof (T).Name}";
			var collectionSettings = new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority };
			_mongoCollectionNames.Add(collectionName);

			return GetMongoDatabase().GetCollection<T>(collectionName, collectionSettings);
		}
		private IList<string> _mongoCollectionNames = new List<string>();
	}
}
