using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestProject1.ef;
using Xunit;

namespace TestProject1
{
    public class Test1
    {
        private DbContextOptions<TestDbContext> ContextOptions;
        private IEnumerable<Student> Samples;

        public Test1()
        {
            ContextOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("MyDb")
                .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;


            Samples = Enumerable.Range(0, 100).Select(i => new Student
            {
                FirstMidName = $"Fname{i}",
                LastName = $"lname{i}"
            });
        }

        [Fact]
        public void Failed_Scoped_EF_Context_ParallelThreads()
        {
            IServiceCollection collection = new ServiceCollection();
            collection.AddScoped(x => new TestDbContext(ContextOptions));
            IServiceProvider provider = collection.BuildServiceProvider(true);
            var context = new TestDbContext(ContextOptions);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            //consider this an http request in an api
            var scope = provider.CreateScope();

            // Act
            Action act = () =>
            {
                // Parallet threads
                Parallel.ForEach(Samples, item =>
                {
                    InsertStudent(scope, item);
                });
            };

            //Assert
            var exception = Assert.Throws<AggregateException>(act);
        }


        [Fact]
        public void Valid_Transient_EF_Context_ParallelThreads()
        {
            IServiceCollection collection = new ServiceCollection();
            collection.AddTransient(x => new TestDbContext(ContextOptions));
            IServiceProvider provider = collection.BuildServiceProvider(true);
            var context = new TestDbContext(ContextOptions);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            //consider this an http request in an api
            var scope = provider.CreateScope();

            // Act
            // Parallet threads
            Parallel.ForEach(Samples, item =>
            {
                InsertStudent(scope, item);
            });

            //Assert
            Assert.Equal(context.Students.Count(), Samples.Count());
        }


        [Fact]
        public void Failed_Scoped_EF_Context_ParallelThreads_Async()
        {
            IServiceCollection collection = new ServiceCollection();
            collection.AddScoped(x => new TestDbContext(ContextOptions));
            IServiceProvider provider = collection.BuildServiceProvider(true);
            var context = new TestDbContext(ContextOptions);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            //consider this an http request in an api
            var scope = provider.CreateScope();

            // Act
            Func<Task> act = async () =>
            {
                // Parallet threads
                await Parallel.ForEachAsync(Samples, async (item, token) =>
                {
                    await InsertStudentAsync(scope, item, token);
                });
            };

            //Assert
            var exception = Assert.ThrowsAsync<AggregateException>(act);
        }


        [Fact]
        public async Task Valid_Transient_EF_Context_ParallelThreads_Async()
        {
            IServiceCollection collection = new ServiceCollection();
            collection.AddTransient(x => new TestDbContext(ContextOptions));
            IServiceProvider provider = collection.BuildServiceProvider(true);
            var context = new TestDbContext(ContextOptions);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            //consider this an http request in an api
            var scope = provider.CreateScope();

            // Act
            // Parallet threads
            await Parallel.ForEachAsync(Samples, async (item, token) =>
            {
                await InsertStudentAsync(scope, item, token);
            });

            //Assert
            Assert.Equal(context.Students.Count(), Samples.Count());
        }

        [Fact]
        public void Failed_Scoped_EF_Context_Tasks_Async()
        {
            IServiceCollection collection = new ServiceCollection();
            collection.AddScoped(x => new TestDbContext(ContextOptions));
            IServiceProvider provider = collection.BuildServiceProvider(true);
            var context = new TestDbContext(ContextOptions);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            //consider this an http request in an api
            var scope = provider.CreateScope();
            CancellationTokenSource source = new CancellationTokenSource();

            // Act
            Func<Task> act = async () =>
            {
                // Parallet tasks
                var tasks = Samples.Select(async ev => await InsertStudentAsync(scope, ev, source.Token));
                await Task.WhenAll(tasks);
            };

            //Assert
            var exception = Assert.ThrowsAsync<AggregateException>(act);
        }


        [Fact]
        public async Task Valid_Transient_EF_Context_Task_Async()
        {
            IServiceCollection collection = new ServiceCollection();
            collection.AddTransient(x => new TestDbContext(ContextOptions));
            IServiceProvider provider = collection.BuildServiceProvider(true);
            var context = new TestDbContext(ContextOptions);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            //consider this an http request in an api
            var scope = provider.CreateScope();
            CancellationTokenSource source = new CancellationTokenSource();

            // Act
            // Parallet tasks
            var tasks = Samples.Select(async ev => await InsertStudentAsync(scope, ev, source.Token));
            await Task.WhenAll(tasks);

            //Assert
            Assert.Equal(context.Students.Count(), Samples.Count());
        }


        private void InsertStudent(IServiceScope scope, Student item)
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            context.Add(item);
            context.SaveChanges();
        }

        private async Task InsertStudentAsync(IServiceScope scope, Student item, CancellationToken token)
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            context.Add(item);
            await context.SaveChangesAsync(token);
        }
    }
}