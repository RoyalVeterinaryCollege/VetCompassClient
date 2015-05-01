using System;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using VetCompass.Client;

namespace VetCompassClient.Tests
{
    /// <summary>
    ///     This test the serialisor by checking that newtonsoft.json can deserialise it correctly
    /// </summary>
    [TestFixture]
    public class SubjectSerialisorTester
    {
        /// <summary>
        ///     Serialises the subject using SubjectSerialisor, then deserialises using newtonsoft.json to assert the
        ///     deserialisation works
        /// </summary>
        /// <param name="subject"></param>
        /// <remarks>This avoids any messy string comparison</remarks>
        private void Assert(CodingSubject subject)
        {
            var serialisor = new SubjectSerialisor(subject);
            var json = serialisor.ToJson();
            var deserialised = JsonConvert.DeserializeObject<CodingSubject>(json);
            deserialised.ShouldBeEquivalentTo(subject);
        }

        [Test]
        public void All_values_Set()
        {
            Assert(new CodingSubject
            {
                CaseNumber = "12313123",
                ApproximateDateOfBirth = DateTime.Now,
                IsFemale = false,
                IsNeutered = true,
                BreedName = "Pointer",
                PartialPostCode = "1231",
                SpeciesName = "Doggy",
                VeNomBreedCode = 23,
                VeNomSpeciesCode = 33
            });
        }

        [Test]
        public void An_empty_subject_should_produce_empty_json()
        {
            Assert(new CodingSubject());
        }

        [Test]
        public void DateTime_serialisation()
        {
            Assert(new CodingSubject {ApproximateDateOfBirth = DateTime.Now});
        }

        [Test]
        public void False_serialisation()
        {
            Assert(new CodingSubject {IsNeutered = false});
        }

        [Test]
        public void integer_serialisation()
        {
            Assert(new CodingSubject {VeNomBreedCode = int.MinValue, VeNomSpeciesCode = int.MaxValue});
        }

        [Test]
        public void Quote_escaping_serialisation()
        {
            Assert(new CodingSubject {CaseNumber = "\"test"});
        }

        [Test]
        public void Simple_CaseNumber_serialisation()
        {
            Assert(new CodingSubject {CaseNumber = "test"});
        }

        [Test]
        public void True_serialisation()
        {
            Assert(new CodingSubject {IsFemale = true});
        }
    }
}