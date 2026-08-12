using System;

namespace QASmartClass.StudentClient.Views.MIGames
{
    public interface IMiGameControl
    {
        event Action<bool, int> OnGameOver;
        void StartGame();
    }
}
