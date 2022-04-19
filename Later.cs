using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Laters {
    /// <summary>Base interface for all Laters</summary>
    public interface ILater { }

    /// <summary>Read-only interface for a <see cref="BaseVoidLater"/></summary>
    public interface IVoidLater : ILater {
        TaskAwaiter GetAwaiter(); 
    }
    
    /// <summary>Read-only interface for a standard <see cref="BaseResultLater{TResult}"/></summary>
    public interface IResultLater<TResult> : ILater {
        TaskAwaiter<TResult> GetAwaiter();
    }

    /// <summary>Standard completable Later which handles a result type</summary>
    public sealed class ResultLater<TResult> : BaseResultLater<TResult> {
        /// <summary>todo</summary>
        public void Complete(TResult result) => _complete(result);
    }

    /// <summary>Standard completable Later with no return</summary>
    public sealed class VoidLater : BaseVoidLater {
        /// <summary>todo</summary>
        public void Complete() => _complete(0);
    }

    /// <summary>Contains the base fields/methods necessary for all derived Laters</summary>
    public abstract class BaseLater<TResult> {
        protected TaskCompletionSource<TResult> _completionSource;
        protected event Action<TResult> _onComplete;
        protected bool _isComplete;
        protected TResult _result;

        protected void _complete(TResult result) {
            if (_isComplete)
                return;

            _result = result;

            var toComplete = _onComplete;
            _onComplete = null;

            _isComplete = true;
            toComplete?.Invoke(_result);
        }
    }
    
    /// <summary>Separated implementation so the derivations can offer alternatives for completion</summary>
    public abstract class BaseVoidLater : BaseLater<byte>, IVoidLater {
        /// <summary>Enables `await` syntax in C#; no need to invoke directly</summary>
        public TaskAwaiter GetAwaiter() {
            if (_completionSource != null)
                return (_completionSource.Task as Task).GetAwaiter();

            _completionSource = new TaskCompletionSource<byte>();
            var awaiter = (_completionSource.Task as Task).GetAwaiter();

            if (_isComplete)
                _completionSource.TrySetResult(0);
            else
                awaiter.OnCompleted(() => _completionSource.TrySetResult(0));

            return awaiter;
        }
    }

    /// <summary>Separated implementation so the derivations can offer different methods for completion</summary>
    public abstract class BaseResultLater<TResult> : BaseLater<TResult>, IResultLater<TResult> {
        /// <summary>Enables `await` syntax in C#; no need to invoke directly</summary>
        public TaskAwaiter<TResult> GetAwaiter() {
            if (_completionSource != null)
                return _completionSource.Task.GetAwaiter();

            _completionSource = new TaskCompletionSource<TResult>();
            var awaiter = _completionSource.Task.GetAwaiter();

            if (_isComplete)
                _completionSource.TrySetResult(_result);
            else
                awaiter.OnCompleted(() => _completionSource.TrySetResult(_result));

            return _completionSource.Task.GetAwaiter();
        }
    }
}