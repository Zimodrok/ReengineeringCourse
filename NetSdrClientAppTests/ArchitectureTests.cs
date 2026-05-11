using NetArchTest.Rules;
using NUnit.Framework;
using System.Reflection;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        private static readonly Assembly ProjectAssembly = typeof(NetSdrClientApp.Messages.NetSdrMessageHelper).Assembly;

        [Test]
        public void Messages_ShouldNot_HaveDependencyOnNetworking()
        {
            // Правило 1: Логіка обробки (Messages) не повинна залежати від інфраструктури (Networking)
            var result = Types.InAssembly(ProjectAssembly)
                .That().ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot().HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, "Архітектурне порушення: Messages не може залежати від Networking!");
        }

        [Test]
        public void NetworkingClasses_Should_HaveWrapperOrClientSuffix()
        {
            // Правило 2: Узгодженість іменування (всі класи в Networking мають закінчуватись на Wrapper або Client)
            var result = Types.InAssembly(ProjectAssembly)
                .That().ResideInNamespace("NetSdrClientApp.Networking")
                .And().AreClasses()
                .Should().HaveNameEndingWith("Wrapper").Or().HaveNameEndingWith("Client")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, "Порушення іменування: класи в Networking повинні мати суфікс Wrapper або Client.");
        }

        [Test]
        public void Interfaces_Should_StartWithI()
        {
            // Правило 3: Стандарт іменування інтерфейсів
            var result = Types.InAssembly(ProjectAssembly)
                .That().AreInterfaces()
                .Should().HaveNameStartingWith("I")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, "Порушення іменування: інтерфейси повинні починатися з літери 'I'.");
        }
    }
}