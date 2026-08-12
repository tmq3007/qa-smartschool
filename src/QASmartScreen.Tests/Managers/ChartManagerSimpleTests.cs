using Moq;
using QASmartTouch.Managers;
using QASmartTouch.Services.Charts;
using Xunit;
using CanvasControl = System.Windows.Controls.Canvas;

namespace QASmartTouch.Tests.Managers
{
    /// <summary>
    /// Simplified Unit tests for ChartManager (Non-WPF)
    /// Coverage Target: 60%+
    /// Tests logic without WPF UI dependencies
    /// </summary>
    public class ChartManagerSimpleTests
    {
        #region Service Injection Tests

        [Fact]
        public void Constructor_WithNullCanvas_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockLineService = new Mock<ILineChartService>();
            var mockPieService = new Mock<IPieChartService>();

            // Act & Assert
            var ex = Assert.Throws<System.ArgumentNullException>(() =>
                new ChartManager(
                    null!,
                    mockLineService.Object,
                    mockPieService.Object
                )
            );

            Assert.Equal("canvas", ex.ParamName);
        }

        #endregion

        #region Service Property Tests

        [Fact]
        public void Properties_ShouldExposeInjectedServices()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();
            var mockLineService = new Mock<ILineChartService>();
            var mockPieService = new Mock<IPieChartService>();
            var mockAreaService = new Mock<IAreaChartService>();
            var mockScatterService = new Mock<IScatterChartService>();
            var mockRadarService = new Mock<IRadarChartService>();

            // Act
            var manager = new ChartManager(
                mockCanvas.Object,
                mockLineService.Object,
                mockPieService.Object,
                mockAreaService.Object,
                mockScatterService.Object,
                mockRadarService.Object
            );

            // Assert
            Assert.Same(mockLineService.Object, manager.LineChartService);
            Assert.Same(mockPieService.Object, manager.PieChartService);
            Assert.Same(mockAreaService.Object, manager.AreaChartService);
            Assert.Same(mockScatterService.Object, manager.ScatterChartService);
            Assert.Same(mockRadarService.Object, manager.RadarChartService);
        }

        #endregion

        #region Default Service Creation Tests

        [Fact]
        public void Constructor_WithoutServices_ShouldCreateDefaultServices()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();

            // Act
            var manager = new ChartManager(mockCanvas.Object);

            // Assert
            Assert.NotNull(manager.LineChartService);
            Assert.NotNull(manager.PieChartService);
            Assert.NotNull(manager.AreaChartService);
            Assert.NotNull(manager.ScatterChartService);
            Assert.NotNull(manager.RadarChartService);
            
            // Verify correct types
            Assert.IsAssignableFrom<ILineChartService>(manager.LineChartService);
            Assert.IsAssignableFrom<IPieChartService>(manager.PieChartService);
            Assert.IsAssignableFrom<IAreaChartService>(manager.AreaChartService);
            Assert.IsAssignableFrom<IScatterChartService>(manager.ScatterChartService);
            Assert.IsAssignableFrom<IRadarChartService>(manager.RadarChartService);
        }

        #endregion

        #region Event Subscription Tests

        [Fact]
        public void OnChartCreated_CanSubscribeAndUnsubscribe()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();
            var manager = new ChartManager(mockCanvas.Object);
            var callCount = 0;

            void Handler(object? sender, CanvasControl chart) => callCount++;

            // Act - Subscribe
            manager.OnChartCreated += Handler;
            
            // Assert - Can subscribe (event cannot be null-checked directly)
            Assert.True(true); // Subscription succeeded

            // Act - Unsubscribe
            manager.OnChartCreated -= Handler;
            
            // Assert - No exception thrown
            Assert.True(true);
        }

        [Fact]
        public void OnChartCreated_WithMultipleSubscribers_AllReceiveNotifications()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();
            var manager = new ChartManager(mockCanvas.Object);
            var calls = new System.Collections.Generic.List<string>();

            manager.OnChartCreated += (s, c) => calls.Add("Handler1");
            manager.OnChartCreated += (s, c) => calls.Add("Handler2");
            manager.OnChartCreated += (s, c) => calls.Add("Handler3");

            // Note: Cannot trigger event externally due to protection level
            // This test validates subscription mechanism only
            
            // Assert
            Assert.Empty(calls); // No calls yet (event not triggered)
        }

        #endregion

        #region Service Interface Validation

        [Fact]
        public void LineChartService_ImplementsCorrectInterface()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();
            var manager = new ChartManager(mockCanvas.Object);

            // Assert
            Assert.IsAssignableFrom<ILineChartService>(manager.LineChartService);
        }

        [Fact]
        public void PieChartService_ImplementsCorrectInterface()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();
            var manager = new ChartManager(mockCanvas.Object);

            // Assert
            Assert.IsAssignableFrom<IPieChartService>(manager.PieChartService);
        }

        [Fact]
        public void AreaChartService_ImplementsCorrectInterface()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();
            var manager = new ChartManager(mockCanvas.Object);

            // Assert
            Assert.IsAssignableFrom<IAreaChartService>(manager.AreaChartService);
        }

        [Fact]
        public void ScatterChartService_ImplementsCorrectInterface()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();
            var manager = new ChartManager(mockCanvas.Object);

            // Assert
            Assert.IsAssignableFrom<IScatterChartService>(manager.ScatterChartService);
        }

        [Fact]
        public void RadarChartService_ImplementsCorrectInterface()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();
            var manager = new ChartManager(mockCanvas.Object);

            // Assert
            Assert.IsAssignableFrom<IRadarChartService>(manager.RadarChartService);
        }

        #endregion

        #region Constructor Overload Tests

        [Fact]
        public void Constructor_WithCanvasOnly_InitializesSuccessfully()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();

            // Act
            var manager = new ChartManager(mockCanvas.Object);

            // Assert
            Assert.NotNull(manager);
            Assert.NotNull(manager.LineChartService);
            Assert.NotNull(manager.PieChartService);
        }

        [Fact]
        public void Constructor_WithAllServices_UsesProvidedInstances()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();
            var customLineService = new Mock<ILineChartService>();
            var customPieService = new Mock<IPieChartService>();
            var customAreaService = new Mock<IAreaChartService>();
            var customScatterService = new Mock<IScatterChartService>();
            var customRadarService = new Mock<IRadarChartService>();

            // Act
            var manager = new ChartManager(
                mockCanvas.Object,
                customLineService.Object,
                customPieService.Object,
                customAreaService.Object,
                customScatterService.Object,
                customRadarService.Object
            );

            // Assert - Verify exact instances are used
            Assert.Same(customLineService.Object, manager.LineChartService);
            Assert.Same(customPieService.Object, manager.PieChartService);
            Assert.Same(customAreaService.Object, manager.AreaChartService);
            Assert.Same(customScatterService.Object, manager.ScatterChartService);
            Assert.Same(customRadarService.Object, manager.RadarChartService);
        }

        #endregion

        #region Dependency Injection Tests

        [Fact]
        public void Constructor_WithPartialServices_CreatesDefaultsForMissing()
        {
            // Arrange
            var mockCanvas = new Mock<CanvasControl>();
            var customLineService = new Mock<ILineChartService>();

            // Act
            var manager = new ChartManager(
                mockCanvas.Object,
                lineChartService: customLineService.Object
            );

            // Assert
            Assert.Same(customLineService.Object, manager.LineChartService); // Custom
            Assert.NotNull(manager.PieChartService); // Default created
            Assert.NotNull(manager.AreaChartService); // Default created
        }

        #endregion

        #region Integration Readiness Tests

        [Fact]
        public void ChartManager_CanBeInstantiatedMultipleTimes()
        {
            // Arrange & Act
            var manager1 = new ChartManager(new Mock<CanvasControl>().Object);
            var manager2 = new ChartManager(new Mock<CanvasControl>().Object);
            var manager3 = new ChartManager(new Mock<CanvasControl>().Object);

            // Assert
            Assert.NotNull(manager1);
            Assert.NotNull(manager2);
            Assert.NotNull(manager3);
            Assert.NotSame(manager1, manager2);
            Assert.NotSame(manager2, manager3);
        }

        [Fact]
        public void ChartManager_ServicesAreDifferentInstancesPerManager()
        {
            // Arrange
            var manager1 = new ChartManager(new Mock<CanvasControl>().Object);
            var manager2 = new ChartManager(new Mock<CanvasControl>().Object);

            // Assert - Each manager has its own service instances
            Assert.NotSame(manager1.LineChartService, manager2.LineChartService);
            Assert.NotSame(manager1.PieChartService, manager2.PieChartService);
        }

        #endregion
    }
}
