using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BenchmarkDotNet.Attributes;
using Cassandra;
using EFCore.Cassandra.Samples;
using EFCore.Cassandra.Samples.Models;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Cassandra.Benchmarks
{
  [Config(typeof(CassandraBenchmarkConfig))]
  public class InsertFixture : IDisposable
  {
    private readonly FakeDbContext _dbContextSut;

    private Applicant[] _data;

    [ParamsSource(nameof(IterationsParams))]
    public int Iterations;

    public InsertFixture()
    {
      _dbContextSut = new FakeDbContext();
    }

    public IEnumerable<int> IterationsParams => new[]
    {
      1_000,
      10_000,
    };
        
    public void Dispose()
    {
      _dbContextSut.Dispose();
    }

    [GlobalSetup]
    public void Setup()
    {
      _dbContextSut.Database.Migrate();

      _data = Enumerable.Range(1, Iterations).Select(_ => BuildFullApplicant()).ToArray();
    }

    [Benchmark]
    public void Add_SaveChanges()
    {
      foreach (var applicant in _data)
      {
        _dbContextSut.Applicants.Add(applicant);
      }

      _dbContextSut.SaveChanges();
    }

    [Benchmark]
    public void AddRange_SaveChanges()
    {
      _dbContextSut.Applicants.AddRange(_data);
      _dbContextSut.SaveChanges();
    }

    private static Applicant BuildFullApplicant()
    {
      var timeUuid = TimeUuid.NewId();
      return new Applicant
      {
        Id = Guid.NewGuid(),
        ApplicantId = Guid.NewGuid(),
        Order = 0,
        Lst = new List<string>
        {
          "1",
          "2"
        },
        LstInt = new List<int>
        {
          1, 2, 3
        },
        Dic = new Dictionary<string, string>
        {
          {"coucou", "coucou"}
        },
        LastName = "lastname",
        BigInteger = 10,
        Bool = false,
        Decimal = 1,
        Double = 2,
        Integer = 3,
        Float = 4,
        Sbyte = 0,
        TimeUuid = timeUuid,
        DateTimeOffset = DateTimeOffset.Now,
        Long = 22,
        SmallInt = 11,
        Blob = new byte[] {1, 2},
        LocalDate = new LocalDate(2019, 10, 2),
        Ip = IPAddress.Loopback,
        LocalTime = new LocalTime(2, 3, 4, 5),
        Address = new ApplicantAddress
        {
          City = "Brussels",
          StreetNumber = 100
        }
      };
    }
  }
}