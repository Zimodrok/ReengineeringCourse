using NetArchTest.Rules;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        [Test]
        public void Messages_ShouldNot_HaveDependencyOnNetworking()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.Messages.NetSdrMessageHelper).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccessful, Is.True, 
                    "Помилка архітектури: Класи з Messages не можуть знати про Networking!");
            });
        }
    }
}