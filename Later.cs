using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Laters {
    public interface ILater<TResult> {
        event Action<TResult> OnComplete;
        TaskAwaiter<TResult> GetAwaiter();
    }
    
    public class Later<TResult> : BaseLater<TResult> {
        public void Complete(TResult result) => _complete(result);
    }

	public abstract class BaseLater<TResult> : ILater<TResult> {
        public event Action<TResult> OnComplete {
            remove => _onComplete -= value;
            add {
                if (_isComplete)
                    _onComplete += value;
                else if (value != null)
                    value(_result);
            }
        }
        
        public TaskAwaiter<TResult> GetAwaiter() {
            if (_completionSource != null) 
                return _completionSource.Task.GetAwaiter();
            
            _completionSource = new TaskCompletionSource<TResult>();
                
            if (_isComplete)
                _completionSource.TrySetResult(_result);
            else
                _onComplete += result => _completionSource.TrySetResult(result);

            return _completionSource.Task.GetAwaiter();
        }

        protected void _complete(TResult result) {
            if (_isComplete)
                return;

            _result = result;
                
            var toComplete = _onComplete; 
            _onComplete = null;
                
            _isComplete = true;
            toComplete?.Invoke(_result);
        }
        
        private TaskCompletionSource<TResult> _completionSource;
        private event Action<TResult> _onComplete;
        private bool _isComplete = false;
        private TResult _result;
	}
}