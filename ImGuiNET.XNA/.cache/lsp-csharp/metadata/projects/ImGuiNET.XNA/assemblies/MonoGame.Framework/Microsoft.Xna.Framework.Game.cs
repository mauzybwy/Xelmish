using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace Microsoft.Xna.Framework;

/// <summary>
/// This class is the entry point for most games. Handles setting up
/// a window and graphics and runs a game loop that calls <see cref="M:Microsoft.Xna.Framework.Game.Update(Microsoft.Xna.Framework.GameTime)" /> and <see cref="M:Microsoft.Xna.Framework.Game.Draw(Microsoft.Xna.Framework.GameTime)" />.
/// </summary>
public class Game : IDisposable
{
	/// <summary>
	/// The SortingFilteringCollection class provides efficient, reusable
	/// sorting and filtering based on a configurable sort comparer, filter
	/// predicate, and associate change events.
	/// </summary>
	private class SortingFilteringCollection<T> : ICollection<T>, IEnumerable<T>, IEnumerable
	{
		private readonly List<T> _items;

		private readonly List<AddJournalEntry<T>> _addJournal;

		private readonly Comparison<AddJournalEntry<T>> _addJournalSortComparison;

		private readonly List<int> _removeJournal;

		private readonly List<T> _cachedFilteredItems;

		private bool _shouldRebuildCache;

		private readonly Predicate<T> _filter;

		private readonly Comparison<T> _sort;

		private readonly Action<T, EventHandler<EventArgs>> _filterChangedSubscriber;

		private readonly Action<T, EventHandler<EventArgs>> _filterChangedUnsubscriber;

		private readonly Action<T, EventHandler<EventArgs>> _sortChangedSubscriber;

		private readonly Action<T, EventHandler<EventArgs>> _sortChangedUnsubscriber;

		private static readonly Comparison<int> RemoveJournalSortComparison = (int x, int y) => Comparer<int>.Default.Compare(y, x);

		public int Count => _items.Count;

		public bool IsReadOnly => false;

		public SortingFilteringCollection(Predicate<T> filter, Action<T, EventHandler<EventArgs>> filterChangedSubscriber, Action<T, EventHandler<EventArgs>> filterChangedUnsubscriber, Comparison<T> sort, Action<T, EventHandler<EventArgs>> sortChangedSubscriber, Action<T, EventHandler<EventArgs>> sortChangedUnsubscriber)
		{
			_items = new List<T>();
			_addJournal = new List<AddJournalEntry<T>>();
			_removeJournal = new List<int>();
			_cachedFilteredItems = new List<T>();
			_shouldRebuildCache = true;
			_filter = filter;
			_filterChangedSubscriber = filterChangedSubscriber;
			_filterChangedUnsubscriber = filterChangedUnsubscriber;
			_sort = sort;
			_sortChangedSubscriber = sortChangedSubscriber;
			_sortChangedUnsubscriber = sortChangedUnsubscriber;
			_addJournalSortComparison = CompareAddJournalEntry;
		}

		private int CompareAddJournalEntry(AddJournalEntry<T> x, AddJournalEntry<T> y)
		{
			int num = _sort(x.Item, y.Item);
			if (num != 0)
			{
				return num;
			}
			return x.Order - y.Order;
		}

		public void ForEachFilteredItem<TUserData>(Action<T, TUserData> action, TUserData userData)
		{
			if (_shouldRebuildCache)
			{
				ProcessRemoveJournal();
				ProcessAddJournal();
				_cachedFilteredItems.Clear();
				for (int i = 0; i < _items.Count; i++)
				{
					if (_filter(_items[i]))
					{
						_cachedFilteredItems.Add(_items[i]);
					}
				}
				_shouldRebuildCache = false;
			}
			for (int j = 0; j < _cachedFilteredItems.Count; j++)
			{
				action(_cachedFilteredItems[j], userData);
			}
			if (_shouldRebuildCache)
			{
				_cachedFilteredItems.Clear();
			}
		}

		public void Add(T item)
		{
			_addJournal.Add(new AddJournalEntry<T>(_addJournal.Count, item));
			InvalidateCache();
		}

		public bool Remove(T item)
		{
			if (_addJournal.Remove(AddJournalEntry<T>.CreateKey(item)))
			{
				return true;
			}
			int num = _items.IndexOf(item);
			if (num >= 0)
			{
				UnsubscribeFromItemEvents(item);
				_removeJournal.Add(num);
				InvalidateCache();
				return true;
			}
			return false;
		}

		public void Clear()
		{
			for (int i = 0; i < _items.Count; i++)
			{
				_filterChangedUnsubscriber(_items[i], Item_FilterPropertyChanged);
				_sortChangedUnsubscriber(_items[i], Item_SortPropertyChanged);
			}
			_addJournal.Clear();
			_removeJournal.Clear();
			_items.Clear();
			InvalidateCache();
		}

		public bool Contains(T item)
		{
			return _items.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_items.CopyTo(array, arrayIndex);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_items).GetEnumerator();
		}

		private void ProcessRemoveJournal()
		{
			if (_removeJournal.Count != 0)
			{
				_removeJournal.Sort(RemoveJournalSortComparison);
				for (int i = 0; i < _removeJournal.Count; i++)
				{
					_items.RemoveAt(_removeJournal[i]);
				}
				_removeJournal.Clear();
			}
		}

		private void ProcessAddJournal()
		{
			if (_addJournal.Count == 0)
			{
				return;
			}
			_addJournal.Sort(_addJournalSortComparison);
			int i = 0;
			for (int j = 0; j < _items.Count; j++)
			{
				if (i >= _addJournal.Count)
				{
					break;
				}
				T item = _addJournal[i].Item;
				if (_sort(item, _items[j]) < 0)
				{
					SubscribeToItemEvents(item);
					_items.Insert(j, item);
					i++;
				}
			}
			for (; i < _addJournal.Count; i++)
			{
				T item2 = _addJournal[i].Item;
				SubscribeToItemEvents(item2);
				_items.Add(item2);
			}
			_addJournal.Clear();
		}

		private void SubscribeToItemEvents(T item)
		{
			_filterChangedSubscriber(item, Item_FilterPropertyChanged);
			_sortChangedSubscriber(item, Item_SortPropertyChanged);
		}

		private void UnsubscribeFromItemEvents(T item)
		{
			_filterChangedUnsubscriber(item, Item_FilterPropertyChanged);
			_sortChangedUnsubscriber(item, Item_SortPropertyChanged);
		}

		private void InvalidateCache()
		{
			_shouldRebuildCache = true;
		}

		private void Item_FilterPropertyChanged(object sender, EventArgs e)
		{
			InvalidateCache();
		}

		private void Item_SortPropertyChanged(object sender, EventArgs e)
		{
			T item = (T)sender;
			int item2 = _items.IndexOf(item);
			_addJournal.Add(new AddJournalEntry<T>(_addJournal.Count, item));
			_removeJournal.Add(item2);
			UnsubscribeFromItemEvents(item);
			InvalidateCache();
		}
	}

	private struct AddJournalEntry<T>
	{
		public readonly int Order;

		public readonly T Item;

		public AddJournalEntry(int order, T item)
		{
			Order = order;
			Item = item;
		}

		public static AddJournalEntry<T> CreateKey(T item)
		{
			return new AddJournalEntry<T>(-1, item);
		}

		public override int GetHashCode()
		{
			return Item.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is AddJournalEntry<T>))
			{
				return false;
			}
			return object.Equals(Item, ((AddJournalEntry<T>)obj).Item);
		}
	}

	private GameComponentCollection _components;

	private GameServiceContainer _services;

	private ContentManager _content;

	internal GamePlatform Platform;

	private SortingFilteringCollection<IDrawable> _drawables = new SortingFilteringCollection<IDrawable>((IDrawable d) => d.Visible, delegate(IDrawable d, EventHandler<EventArgs> handler)
	{
		d.VisibleChanged += handler;
	}, delegate(IDrawable d, EventHandler<EventArgs> handler)
	{
		d.VisibleChanged -= handler;
	}, (IDrawable d1, IDrawable d2) => Comparer<int>.Default.Compare(d1.DrawOrder, d2.DrawOrder), delegate(IDrawable d, EventHandler<EventArgs> handler)
	{
		d.DrawOrderChanged += handler;
	}, delegate(IDrawable d, EventHandler<EventArgs> handler)
	{
		d.DrawOrderChanged -= handler;
	});

	private SortingFilteringCollection<IUpdateable> _updateables = new SortingFilteringCollection<IUpdateable>((IUpdateable u) => u.Enabled, delegate(IUpdateable u, EventHandler<EventArgs> handler)
	{
		u.EnabledChanged += handler;
	}, delegate(IUpdateable u, EventHandler<EventArgs> handler)
	{
		u.EnabledChanged -= handler;
	}, (IUpdateable u1, IUpdateable u2) => Comparer<int>.Default.Compare(u1.UpdateOrder, u2.UpdateOrder), delegate(IUpdateable u, EventHandler<EventArgs> handler)
	{
		u.UpdateOrderChanged += handler;
	}, delegate(IUpdateable u, EventHandler<EventArgs> handler)
	{
		u.UpdateOrderChanged -= handler;
	});

	private IGraphicsDeviceManager _graphicsDeviceManager;

	private IGraphicsDeviceService _graphicsDeviceService;

	private bool _initialized;

	private bool _isFixedTimeStep = true;

	private TimeSpan _targetElapsedTime = TimeSpan.FromTicks(166667L);

	private TimeSpan _inactiveSleepTime = TimeSpan.FromSeconds(0.02);

	private TimeSpan _maxElapsedTime = TimeSpan.FromMilliseconds(500.0);

	private bool _shouldExit;

	private bool _suppressDraw;

	private bool _isDisposed;

	private static Game _instance = null;

	private TimeSpan _accumulatedElapsedTime;

	private readonly GameTime _gameTime = new GameTime();

	private Stopwatch _gameTimer;

	private long _previousTicks;

	private int _updateFrameLag;

	private static readonly Action<IDrawable, GameTime> DrawAction = delegate(IDrawable drawable, GameTime gameTime)
	{
		drawable.Draw(gameTime);
	};

	private static readonly Action<IUpdateable, GameTime> UpdateAction = delegate(IUpdateable updateable, GameTime gameTime)
	{
		updateable.Update(gameTime);
	};

	internal static Game Instance => _instance;

	/// <summary>
	/// The start up parameters for this <see cref="T:Microsoft.Xna.Framework.Game" />.
	/// </summary>
	public LaunchParameters LaunchParameters { get; private set; }

	/// <summary>
	/// A collection of game components attached to this <see cref="T:Microsoft.Xna.Framework.Game" />.
	/// </summary>
	public GameComponentCollection Components => _components;

	public TimeSpan InactiveSleepTime
	{
		get
		{
			return _inactiveSleepTime;
		}
		set
		{
			if (value < TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("The time must be positive.", (Exception?)null);
			}
			_inactiveSleepTime = value;
		}
	}

	/// <summary>
	/// The maximum amount of time we will frameskip over and only perform Update calls with no Draw calls.
	/// MonoGame extension.
	/// </summary>
	public TimeSpan MaxElapsedTime
	{
		get
		{
			return _maxElapsedTime;
		}
		set
		{
			if (value < TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("The time must be positive.", (Exception?)null);
			}
			if (value < _targetElapsedTime)
			{
				throw new ArgumentOutOfRangeException("The time must be at least TargetElapsedTime", (Exception?)null);
			}
			_maxElapsedTime = value;
		}
	}

	/// <summary>
	/// Indicates if the game is the focused application.
	/// </summary>
	public bool IsActive => Platform.IsActive;

	/// <summary>
	/// Indicates if the mouse cursor is visible on the game screen.
	/// </summary>
	public bool IsMouseVisible
	{
		get
		{
			return Platform.IsMouseVisible;
		}
		set
		{
			Platform.IsMouseVisible = value;
		}
	}

	/// <summary>
	/// The time between frames when running with a fixed time step. <seealso cref="P:Microsoft.Xna.Framework.Game.IsFixedTimeStep" />
	/// </summary>
	/// <exception cref="T:System.ArgumentOutOfRangeException">Target elapsed time must be strictly larger than zero.</exception>
	public TimeSpan TargetElapsedTime
	{
		get
		{
			return _targetElapsedTime;
		}
		set
		{
			value = Platform.TargetElapsedTimeChanging(value);
			if (value <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("The time must be positive and non-zero.", (Exception?)null);
			}
			if (value != _targetElapsedTime)
			{
				_targetElapsedTime = value;
				Platform.TargetElapsedTimeChanged();
			}
		}
	}

	/// <summary>
	/// Indicates if this game is running with a fixed time between frames.
	///
	/// When set to <code>true</code> the target time between frames is
	/// given by <see cref="P:Microsoft.Xna.Framework.Game.TargetElapsedTime" />.
	/// </summary>
	public bool IsFixedTimeStep
	{
		get
		{
			return _isFixedTimeStep;
		}
		set
		{
			_isFixedTimeStep = value;
		}
	}

	/// <summary>
	/// Get a container holding service providers attached to this <see cref="T:Microsoft.Xna.Framework.Game" />.
	/// </summary>
	public GameServiceContainer Services => _services;

	/// <summary>
	/// The <see cref="T:Microsoft.Xna.Framework.Content.ContentManager" /> of this <see cref="T:Microsoft.Xna.Framework.Game" />.
	/// </summary>
	/// <exception cref="T:System.ArgumentNullException">If Content is set to <code>null</code>.</exception>
	public ContentManager Content
	{
		get
		{
			return _content;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException();
			}
			_content = value;
		}
	}

	/// <summary>
	/// Gets the <see cref="P:Microsoft.Xna.Framework.Game.GraphicsDevice" /> used for rendering by this <see cref="T:Microsoft.Xna.Framework.Game" />.
	/// </summary>
	/// <exception cref="T:System.InvalidOperationException">
	/// There is no <see cref="T:Microsoft.Xna.Framework.Graphics.GraphicsDevice" /> attached to this <see cref="T:Microsoft.Xna.Framework.Game" />.
	/// </exception>
	public GraphicsDevice GraphicsDevice
	{
		get
		{
			if (_graphicsDeviceService == null)
			{
				_graphicsDeviceService = (IGraphicsDeviceService)Services.GetService(typeof(IGraphicsDeviceService));
				if (_graphicsDeviceService == null)
				{
					throw new InvalidOperationException("No Graphics Device Service");
				}
			}
			return _graphicsDeviceService.GraphicsDevice;
		}
	}

	/// <summary>
	/// The system window that this game is displayed on.
	/// </summary>
	[CLSCompliant(false)]
	public GameWindow Window => Platform.Window;

	internal bool Initialized => _initialized;

	internal GraphicsDeviceManager graphicsDeviceManager
	{
		get
		{
			if (_graphicsDeviceManager == null)
			{
				_graphicsDeviceManager = (IGraphicsDeviceManager)Services.GetService(typeof(IGraphicsDeviceManager));
			}
			return (GraphicsDeviceManager)_graphicsDeviceManager;
		}
		set
		{
			if (_graphicsDeviceManager != null)
			{
				throw new InvalidOperationException("GraphicsDeviceManager already registered for this Game object");
			}
			_graphicsDeviceManager = value;
		}
	}

	/// <summary>
	/// Raised when the game gains focus.
	/// </summary>
	public event EventHandler<EventArgs> Activated;

	/// <summary>
	/// Raised when the game loses focus.
	/// </summary>
	public event EventHandler<EventArgs> Deactivated;

	/// <summary>
	/// Raised when this game is being disposed.
	/// </summary>
	public event EventHandler<EventArgs> Disposed;

	/// <summary>
	/// Raised when this game is exiting.
	/// </summary>
	public event EventHandler<EventArgs> Exiting;

	/// <summary>
	/// Create a <see cref="T:Microsoft.Xna.Framework.Game" />.
	/// </summary>
	public Game()
	{
		_instance = this;
		LaunchParameters = new LaunchParameters();
		_services = new GameServiceContainer();
		_components = new GameComponentCollection();
		_content = new ContentManager(_services);
		Platform = GamePlatform.PlatformCreate(this);
		Platform.Activated += OnActivated;
		Platform.Deactivated += OnDeactivated;
		_services.AddService(typeof(GamePlatform), Platform);
		FrameworkDispatcher.Update();
	}

	~Game()
	{
		Dispose(disposing: false);
	}

	[Conditional("DEBUG")]
	internal void Log(string Message)
	{
		_ = Platform;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
		EventHelpers.Raise(this, this.Disposed, EventArgs.Empty);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_isDisposed)
		{
			return;
		}
		if (disposing)
		{
			for (int i = 0; i < _components.Count; i++)
			{
				if (_components[i] is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			_components = null;
			if (_content != null)
			{
				_content.Dispose();
				_content = null;
			}
			if (_graphicsDeviceManager != null)
			{
				(_graphicsDeviceManager as GraphicsDeviceManager).Dispose();
				_graphicsDeviceManager = null;
			}
			if (Platform != null)
			{
				Platform.Activated -= OnActivated;
				Platform.Deactivated -= OnDeactivated;
				_services.RemoveService(typeof(GamePlatform));
				Platform.Dispose();
				Platform = null;
			}
			ContentTypeReaderManager.ClearTypeCreators();
			if (SoundEffect._systemState == SoundEffect.SoundSystemState.Initialized)
			{
				SoundEffect.PlatformShutdown();
			}
		}
		_isDisposed = true;
		_instance = null;
	}

	[DebuggerNonUserCode]
	private void AssertNotDisposed()
	{
		if (_isDisposed)
		{
			string name = GetType().Name;
			throw new ObjectDisposedException(name, $"The {name} object was used after being Disposed.");
		}
	}

	/// <summary>
	/// Exit the game at the end of this tick.
	/// </summary>
	public void Exit()
	{
		_shouldExit = true;
		_suppressDraw = true;
	}

	/// <summary>
	/// Reset the elapsed game time to <see cref="F:System.TimeSpan.Zero" />.
	/// </summary>
	public void ResetElapsedTime()
	{
		Platform.ResetElapsedTime();
		_gameTimer.Reset();
		_gameTimer.Start();
		_accumulatedElapsedTime = TimeSpan.Zero;
		_gameTime.ElapsedGameTime = TimeSpan.Zero;
		_previousTicks = 0L;
	}

	/// <summary>
	/// Supress calling <see cref="M:Microsoft.Xna.Framework.Game.Draw(Microsoft.Xna.Framework.GameTime)" /> in the game loop.
	/// </summary>
	public void SuppressDraw()
	{
		_suppressDraw = true;
	}

	/// <summary>
	/// Run the game for one frame, then exit.
	/// </summary>
	public void RunOneFrame()
	{
		if (Platform != null && Platform.BeforeRun())
		{
			if (!_initialized)
			{
				DoInitialize();
				_gameTimer = Stopwatch.StartNew();
				_initialized = true;
			}
			BeginRun();
			Tick();
			EndRun();
		}
	}

	/// <summary>
	/// Run the game using the default <see cref="T:Microsoft.Xna.Framework.GameRunBehavior" /> for the current platform.
	/// </summary>
	public void Run()
	{
		Run(Platform.DefaultRunBehavior);
	}

	/// <summary>
	/// Run the game.
	/// </summary>
	/// <param name="runBehavior">Indicate if the game should be run synchronously or asynchronously.</param>
	public void Run(GameRunBehavior runBehavior)
	{
		AssertNotDisposed();
		if (!Platform.BeforeRun())
		{
			BeginRun();
			_gameTimer = Stopwatch.StartNew();
			return;
		}
		if (!_initialized)
		{
			DoInitialize();
			_initialized = true;
		}
		BeginRun();
		_gameTimer = Stopwatch.StartNew();
		switch (runBehavior)
		{
		case GameRunBehavior.Asynchronous:
			Platform.AsyncRunLoopEnded += Platform_AsyncRunLoopEnded;
			Platform.StartRunLoop();
			break;
		case GameRunBehavior.Synchronous:
			DoUpdate(new GameTime());
			Platform.RunLoop();
			EndRun();
			DoExiting();
			break;
		default:
			throw new ArgumentException($"Handling for the run behavior {runBehavior} is not implemented.");
		}
	}

	/// <summary>
	/// Run one iteration of the game loop.
	///
	/// Makes at least one call to <see cref="M:Microsoft.Xna.Framework.Game.Update(Microsoft.Xna.Framework.GameTime)" />
	/// and exactly one call to <see cref="M:Microsoft.Xna.Framework.Game.Draw(Microsoft.Xna.Framework.GameTime)" /> if drawing is not supressed.
	/// When <see cref="P:Microsoft.Xna.Framework.Game.IsFixedTimeStep" /> is set to <code>false</code> this will
	/// make exactly one call to <see cref="M:Microsoft.Xna.Framework.Game.Update(Microsoft.Xna.Framework.GameTime)" />.
	/// </summary>
	public void Tick()
	{
		while (true)
		{
			if (!IsActive && InactiveSleepTime.TotalMilliseconds >= 1.0)
			{
				Thread.Sleep((int)InactiveSleepTime.TotalMilliseconds);
			}
			long ticks = _gameTimer.Elapsed.Ticks;
			_accumulatedElapsedTime += TimeSpan.FromTicks(ticks - _previousTicks);
			_previousTicks = ticks;
			if (!IsFixedTimeStep || !(_accumulatedElapsedTime < TargetElapsedTime))
			{
				break;
			}
			if ((TargetElapsedTime - _accumulatedElapsedTime).TotalMilliseconds >= 2.0)
			{
				Thread.Sleep(1);
			}
		}
		if (_accumulatedElapsedTime > _maxElapsedTime)
		{
			_accumulatedElapsedTime = _maxElapsedTime;
		}
		if (IsFixedTimeStep)
		{
			_gameTime.ElapsedGameTime = TargetElapsedTime;
			int num = 0;
			while (_accumulatedElapsedTime >= TargetElapsedTime && !_shouldExit)
			{
				_gameTime.TotalGameTime += TargetElapsedTime;
				_accumulatedElapsedTime -= TargetElapsedTime;
				num++;
				DoUpdate(_gameTime);
			}
			_updateFrameLag += Math.Max(0, num - 1);
			if (_gameTime.IsRunningSlowly)
			{
				if (_updateFrameLag == 0)
				{
					_gameTime.IsRunningSlowly = false;
				}
			}
			else if (_updateFrameLag >= 5)
			{
				_gameTime.IsRunningSlowly = true;
			}
			if (num == 1 && _updateFrameLag > 0)
			{
				_updateFrameLag--;
			}
			_gameTime.ElapsedGameTime = TimeSpan.FromTicks(TargetElapsedTime.Ticks * num);
		}
		else
		{
			_gameTime.ElapsedGameTime = _accumulatedElapsedTime;
			_gameTime.TotalGameTime += _accumulatedElapsedTime;
			_accumulatedElapsedTime = TimeSpan.Zero;
			DoUpdate(_gameTime);
		}
		if (_suppressDraw)
		{
			_suppressDraw = false;
		}
		else
		{
			DoDraw(_gameTime);
		}
		if (_shouldExit)
		{
			Platform.Exit();
			_shouldExit = false;
		}
	}

	/// <summary>
	/// Called right before <see cref="M:Microsoft.Xna.Framework.Game.Draw(Microsoft.Xna.Framework.GameTime)" /> is normally called. Can return <code>false</code>
	/// to let the game loop not call <see cref="M:Microsoft.Xna.Framework.Game.Draw(Microsoft.Xna.Framework.GameTime)" />.
	/// </summary>
	/// <returns>
	///   <code>true</code> if <see cref="M:Microsoft.Xna.Framework.Game.Draw(Microsoft.Xna.Framework.GameTime)" /> should be called, <code>false</code> if it should not.
	/// </returns>
	protected virtual bool BeginDraw()
	{
		return true;
	}

	/// <summary>
	/// Called right after <see cref="M:Microsoft.Xna.Framework.Game.Draw(Microsoft.Xna.Framework.GameTime)" />. Presents the
	/// rendered frame in the <see cref="T:Microsoft.Xna.Framework.GameWindow" />.
	/// </summary>
	protected virtual void EndDraw()
	{
		Platform.Present();
	}

	/// <summary>
	/// Called after <see cref="M:Microsoft.Xna.Framework.Game.Initialize" />, but before the first call to <see cref="M:Microsoft.Xna.Framework.Game.Update(Microsoft.Xna.Framework.GameTime)" />.
	/// </summary>
	protected virtual void BeginRun()
	{
	}

	/// <summary>
	/// Called when the game loop has been terminated before exiting.
	/// </summary>
	protected virtual void EndRun()
	{
	}

	/// <summary>
	/// Override this to load graphical resources required by the game.
	/// </summary>
	protected virtual void LoadContent()
	{
	}

	/// <summary>
	/// Override this to unload graphical resources loaded by the game.
	/// </summary>
	protected virtual void UnloadContent()
	{
	}

	/// <summary>
	/// Override this to initialize the game and load any needed non-graphical resources.
	///
	/// Initializes attached <see cref="T:Microsoft.Xna.Framework.GameComponent" /> instances and calls <see cref="M:Microsoft.Xna.Framework.Game.LoadContent" />.
	/// </summary>
	protected virtual void Initialize()
	{
		applyChanges(graphicsDeviceManager);
		InitializeExistingComponents();
		_graphicsDeviceService = (IGraphicsDeviceService)Services.GetService(typeof(IGraphicsDeviceService));
		if (_graphicsDeviceService != null && _graphicsDeviceService.GraphicsDevice != null)
		{
			LoadContent();
		}
	}

	/// <summary>
	/// Called when the game should draw a frame.
	///
	/// Draws the <see cref="T:Microsoft.Xna.Framework.DrawableGameComponent" /> instances attached to this game.
	/// Override this to render your game.
	/// </summary>
	/// <param name="gameTime">A <see cref="T:Microsoft.Xna.Framework.GameTime" /> instance containing the elapsed time since the last call to <see cref="M:Microsoft.Xna.Framework.Game.Draw(Microsoft.Xna.Framework.GameTime)" /> and the total time elapsed since the game started.</param>
	protected virtual void Draw(GameTime gameTime)
	{
		_drawables.ForEachFilteredItem(DrawAction, gameTime);
	}

	/// <summary>
	/// Called when the game should update.
	///
	/// Updates the <see cref="T:Microsoft.Xna.Framework.GameComponent" /> instances attached to this game.
	/// Override this to update your game.
	/// </summary>
	/// <param name="gameTime">The elapsed time since the last call to <see cref="M:Microsoft.Xna.Framework.Game.Update(Microsoft.Xna.Framework.GameTime)" />.</param>
	protected virtual void Update(GameTime gameTime)
	{
		_updateables.ForEachFilteredItem(UpdateAction, gameTime);
	}

	/// <summary>
	/// Called when the game is exiting. Raises the <see cref="E:Microsoft.Xna.Framework.Game.Exiting" /> event.
	/// </summary>
	/// <param name="sender">This <see cref="T:Microsoft.Xna.Framework.Game" />.</param>
	/// <param name="args">The arguments to the <see cref="E:Microsoft.Xna.Framework.Game.Exiting" /> event.</param>
	protected virtual void OnExiting(object sender, EventArgs args)
	{
		EventHelpers.Raise(sender, this.Exiting, args);
	}

	/// <summary>
	/// Called when the game gains focus. Raises the <see cref="E:Microsoft.Xna.Framework.Game.Activated" /> event.
	/// </summary>
	/// <param name="sender">This <see cref="T:Microsoft.Xna.Framework.Game" />.</param>
	/// <param name="args">The arguments to the <see cref="E:Microsoft.Xna.Framework.Game.Activated" /> event.</param>
	protected virtual void OnActivated(object sender, EventArgs args)
	{
		AssertNotDisposed();
		EventHelpers.Raise(sender, this.Activated, args);
	}

	/// <summary>
	/// Called when the game loses focus. Raises the <see cref="E:Microsoft.Xna.Framework.Game.Deactivated" /> event.
	/// </summary>
	/// <param name="sender">This <see cref="T:Microsoft.Xna.Framework.Game" />.</param>
	/// <param name="args">The arguments to the <see cref="E:Microsoft.Xna.Framework.Game.Deactivated" /> event.</param>
	protected virtual void OnDeactivated(object sender, EventArgs args)
	{
		AssertNotDisposed();
		EventHelpers.Raise(sender, this.Deactivated, args);
	}

	private void Components_ComponentAdded(object sender, GameComponentCollectionEventArgs e)
	{
		e.GameComponent.Initialize();
		CategorizeComponent(e.GameComponent);
	}

	private void Components_ComponentRemoved(object sender, GameComponentCollectionEventArgs e)
	{
		DecategorizeComponent(e.GameComponent);
	}

	private void Platform_AsyncRunLoopEnded(object sender, EventArgs e)
	{
		AssertNotDisposed();
		((GamePlatform)sender).AsyncRunLoopEnded -= Platform_AsyncRunLoopEnded;
		EndRun();
		DoExiting();
	}

	internal void applyChanges(GraphicsDeviceManager manager)
	{
		Platform.BeginScreenDeviceChange(GraphicsDevice.PresentationParameters.IsFullScreen);
		if (GraphicsDevice.PresentationParameters.IsFullScreen)
		{
			Platform.EnterFullScreen();
		}
		else
		{
			Platform.ExitFullScreen();
		}
		Viewport viewport = new Viewport(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
		GraphicsDevice.Viewport = viewport;
		Platform.EndScreenDeviceChange(string.Empty, viewport.Width, viewport.Height);
	}

	internal void DoUpdate(GameTime gameTime)
	{
		AssertNotDisposed();
		if (Platform.BeforeUpdate(gameTime))
		{
			FrameworkDispatcher.Update();
			Update(gameTime);
			TouchPanelState.CurrentTimestamp = gameTime.TotalGameTime;
		}
	}

	internal void DoDraw(GameTime gameTime)
	{
		AssertNotDisposed();
		if (Platform.BeforeDraw(gameTime) && BeginDraw())
		{
			Draw(gameTime);
			EndDraw();
		}
	}

	internal void DoInitialize()
	{
		AssertNotDisposed();
		if (GraphicsDevice == null && graphicsDeviceManager != null)
		{
			_graphicsDeviceManager.CreateDevice();
		}
		Platform.BeforeInitialize();
		Initialize();
		CategorizeComponents();
		_components.ComponentAdded += Components_ComponentAdded;
		_components.ComponentRemoved += Components_ComponentRemoved;
	}

	internal void DoExiting()
	{
		OnExiting(this, EventArgs.Empty);
		UnloadContent();
	}

	private void InitializeExistingComponents()
	{
		for (int i = 0; i < Components.Count; i++)
		{
			Components[i].Initialize();
		}
	}

	private void CategorizeComponents()
	{
		DecategorizeComponents();
		for (int i = 0; i < Components.Count; i++)
		{
			CategorizeComponent(Components[i]);
		}
	}

	private void DecategorizeComponents()
	{
		_updateables.Clear();
		_drawables.Clear();
	}

	private void CategorizeComponent(IGameComponent component)
	{
		if (component is IUpdateable)
		{
			_updateables.Add((IUpdateable)component);
		}
		if (component is IDrawable)
		{
			_drawables.Add((IDrawable)component);
		}
	}

	private void DecategorizeComponent(IGameComponent component)
	{
		if (component is IUpdateable)
		{
			_updateables.Remove((IUpdateable)component);
		}
		if (component is IDrawable)
		{
			_drawables.Remove((IDrawable)component);
		}
	}
}
