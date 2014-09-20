﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using FluentNHibernate.Cfg;
using NameWithPersistence;
using FluentNHibernate.Cfg.Db;
using NHibernate.Cfg;
using System.IO;
using NHibernate.Tool.hbm2ddl;
using FluentNHibernate.Mapping;
using NHibernate.UserTypes;
using System.Data;
using NHibernate.SqlTypes;

namespace Tests
{
    [TestClass]
    public class Denormalized_persistence_example
    {
        ISessionFactory sessionFactory = CreateSessionFactory();

        [TestMethod]
        public void Persisting_denormalized_value_objects()
        {
            Guid id = Guid.NewGuid();

            NHibernateTransaction(session => 
            {
                var name = new Name("Kevin", "Kingston");
                var customer = new Customer(id,  name);
                session.Save(customer, id);
            });

            NHibernateTransaction(session =>
            {
                var customer = session.Get<Customer>(id);

                var name = new Name("Kevin", "Kingston");
                Assert.AreEqual(name, customer.Name);
            });
        }

        private void NHibernateTransaction(Action<ISession> action)
        {
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                action(session);
                transaction.Commit();
            };
        }

        // See fluent NHibernate docs for more info: https://github.com/jagregory/fluent-nhibernate/wiki/Getting-started
        private static ISessionFactory CreateSessionFactory()
        {
            // ** Add your connection string here
            // Quickest option is to use Server Exploer -> Data Connections -> Add Data Connection -> Microsoft SQL Server Database File
            // This will create the file for you. Right-click on it and choose properties to get the connection string
            var connString = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=C:\Users\John\Documents\valueobjectsdb.mdf;Integrated Security=True;Connect Timeout=30";
           
            // Manually configuring Map. NHibernate supports conventions and advanced features as well
            return Fluently.Configure()
                           .Mappings(x => x.FluentMappings.Add(typeof(CustomerMap)))
                           .Database(
                                MsSqlConfiguration.MsSql2005.ConnectionString(connString)
                            )
                           .ExposeConfiguration(BuildSchema)
                           .BuildSessionFactory();
        }

        private static void BuildSchema(Configuration config)
        {
            new SchemaExport(config).Create(false, true);
        }
    }

    // Fluent NHibernate mapping class
    public class CustomerMap : ClassMap<Customer>
    {
        public CustomerMap()
        {
            Id(x => x.Id);
            Map(x => x.Name).CustomType<NameValueObjectPersister>();
        }
    }


    // NHibernate customisation for custom mapping logic
    public class NameValueObjectPersister : IUserType
    {
        private const int MaxPersistedLength = 100;

        public SqlType[] SqlTypes
        {
            get
            {
                SqlType[] types = new SqlType[1];
                types[0] = new SqlType(DbType.String);
                return types;
            }
        }

        public Type ReturnedType
        {
            get { return typeof(Name); }
        }

        public bool IsMutable
        {
            get { return false; }
        }

        public int GetHashCode(object x)
        {
            if (x == null)
            {
                return 0;
            }
            return x.GetHashCode();
        }

        public object NullSafeGet(IDataReader rs, string[] names, object owner)
        {
            object storageRepresenation = NHibernateUtil.String.NullSafeGet(rs, names[0]);
            if (storageRepresenation == null)
            {
                return null;
            }

            // storage representation format: firstName:{X};;surName:{Y}
            var parts = storageRepresenation.ToString().Split(new[] { ";;" }, StringSplitOptions.None);
            var firstName = parts[0].Split(':')[1];
            var surName = parts[1].Split(':')[1];

            return new Name(firstName, surName);
        }

        public void NullSafeSet(IDbCommand cmd, object value, int index)
        {
            var parameter = (IDataParameter)cmd.Parameters[index];
            if (value == null)
            {
                parameter.Value = DBNull.Value;
            }
            else
            {
                parameter.Value = value.ToString();
            }
        }

        public object DeepCopy(object value)
        {
            // we can ignore it...
            return value;
        }

        public object Replace(object original, object target, object owner)
        {
            // we can ignore it...
            return original;
        }

        public object Assemble(object cached, object owner)
        {
            // we can ignore it...
            return cached;
        }

        public object Disassemble(object value)
        {
            // we can ignore it...
            return value;
        }

        bool IUserType.Equals(object x, object y)
        {
            return x.Equals(y);
        }
    }

}
