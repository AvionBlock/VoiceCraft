using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels;

namespace VoiceCraft.Client.Tests.Services;

public class NavigationServiceTests
{
    [Fact]
    public void NavigateTo_RaisesCurrentViewModelAndLifecycleEvents()
    {
        var first = new TestViewModel();
        var second = new TestViewModel();
        var service = CreateService(first, second);
        ViewModelBase? current = null;

        service.OnViewModelChanged += viewModel => current = viewModel;

        service.NavigateTo<TestViewModel>("payload");

        Assert.Same(first, current);
        Assert.Equal(1, first.AppearingCalls);
        Assert.Equal("payload", first.LastAppearingData);
        Assert.False(service.HasPrev);
        Assert.False(service.HasNext);
    }

    [Fact]
    public void BackAndForward_MoveThroughHistory()
    {
        var first = new TestViewModel();
        var second = new TestViewModel();
        var third = new TestViewModel();
        var service = CreateService(first, second, third);

        service.NavigateTo<TestViewModel>("first");
        service.NavigateTo<TestViewModel>("second");
        service.NavigateTo<TestViewModel>("third");

        var back = service.Back();
        var forward = service.Forward();

        Assert.Same(second, back);
        Assert.Same(third, forward);
        Assert.Equal(2, second.AppearingCalls);
        Assert.Equal(2, third.AppearingCalls);
        Assert.True(service.HasPrev);
        Assert.False(service.HasNext);
    }

    [Fact]
    public void NavigateTo_AfterBack_DiscardsForwardHistoryAndDisposesEntries()
    {
        var first = new DisposableTestViewModel();
        var second = new DisposableTestViewModel();
        var third = new DisposableTestViewModel();
        var replacement = new DisposableTestViewModel();
        var service = CreateService(first, second, third, replacement);

        service.NavigateTo<DisposableTestViewModel>();
        service.NavigateTo<DisposableTestViewModel>();
        service.NavigateTo<DisposableTestViewModel>();

        service.Back();
        service.NavigateTo<DisposableTestViewModel>();

        Assert.Equal(1, third.DisposeCalls);
        Assert.False(service.HasNext);
        Assert.True(service.HasPrev);
    }

    [Fact]
    public void Back_WithDisabledBackButton_ReturnsCurrentViewModel()
    {
        var current = new BlockingViewModel();
        var previous = new TestViewModel();
        var service = CreateService(previous, current);

        service.NavigateTo<TestViewModel>();
        service.NavigateTo<BlockingViewModel>();

        var result = service.Back(checkBackButton: true);

        Assert.Same(current, result);
        Assert.Equal(1, previous.AppearingCalls);
        Assert.Equal(1, current.AppearingCalls);
    }

    [Fact]
    public void NavigateTo_OverHistoryLimit_DisposesOldestEntry()
    {
        var first = new DisposableTestViewModel();
        var second = new DisposableTestViewModel();
        var third = new DisposableTestViewModel();
        var service = CreateService(new ViewModelBase[] { first, second, third }, historyMaxSize: 2);

        service.NavigateTo<DisposableTestViewModel>();
        service.NavigateTo<DisposableTestViewModel>();
        service.NavigateTo<DisposableTestViewModel>();

        Assert.Equal(1, first.DisposeCalls);
        Assert.True(service.HasPrev);
        Assert.False(service.HasNext);
        Assert.Null(service.Go(-2));
    }

    private static NavigationService CreateService(params ViewModelBase[] viewModels)
    {
        return CreateService(viewModels, 100);
    }

    private static NavigationService CreateService(IEnumerable<ViewModelBase> viewModels, uint historyMaxSize)
    {
        var queue = new Queue<ViewModelBase>(viewModels);
        return new NavigationService(_ => queue.Dequeue(), historyMaxSize);
    }

    private class TestViewModel : ViewModelBase
    {
        public int AppearingCalls { get; private set; }
        public int DisappearingCalls { get; private set; }
        public object? LastAppearingData { get; private set; }

        public override void OnAppearing(object? data = null)
        {
            AppearingCalls++;
            LastAppearingData = data;
        }

        public override void OnDisappearing()
        {
            DisappearingCalls++;
        }
    }

    private sealed class BlockingViewModel : ViewModelBase
    {
        public override bool DisableBackButton { get; protected set; } = true;
        public int AppearingCalls { get; private set; }

        public override void OnAppearing(object? data = null)
        {
            AppearingCalls++;
        }
    }

    private sealed class DisposableTestViewModel : TestViewModel, IDisposable
    {
        public int DisposeCalls { get; private set; }

        public void Dispose()
        {
            DisposeCalls++;
        }
    }
}
