using System.Threading;

namespace Sackrany.GameInput.SackranyInput
{
    /// <summary>
    /// Контракт между ручным <see cref="InputManager"/> и сгенерированной схемой ввода.
    /// Сгенерированный класс реализует это и сам регистрируется в InputManager на старте,
    /// поэтому ручной код не зависит от генерёнки и нигде нет partial-классов между сборками.
    /// </summary>
    public interface IInputBinding
    {
        /// <summary>Создать схему ввода и кэши, привязать к токену жизни InputManager.</summary>
        void Init(CancellationToken token);

        /// <summary>Выгрузить схему и отключить карты действий.</summary>
        void Dispose();

        /// <summary>Применить оверрайды бинда из конфига к asset схемы.</summary>
        void ApplySettings();
    }
}
