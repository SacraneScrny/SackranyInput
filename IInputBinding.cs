using System.Threading;

namespace SackranyInput
{
    public interface IInputBinding
    {
        void Init(CancellationToken token);

        void Dispose();

        void ApplySettings();
    }
}
