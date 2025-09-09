using System;
using System.Threading.Tasks;
using com.appidea.MiniGamePlatform.CommunicationAPI;
using UnityEngine;

public class PaintGameEntryPoint : BaseMiniGameEntryPoint
{
    [SerializeField] private GameObject gamePrefab;
    private IGameOverScreen gameOverScreen = default;
    private GameOverScreenData GameOverData => GetGameOverScreenData;

    protected override Task LoadInternal()
    {
        var gameManager = Instantiate(gamePrefab);
        gameManager.GetComponent<APISystem>().SetEntryPoint(this);

        var canvasParent = gameManager.GetComponent<APISystem>();

        if (HasOverridenGameOverScreen)
        {
            gameOverScreen =
                Instantiate(GetGameOverScreenData.Prefab.Transform(), canvasParent.MainCanvas.transform)
                    .GetComponent<IGameOverScreen>();

            gameOverScreen.Init(GameOverData.CurrentMiniGame, GameOverData.RecommendedMiniGames);
            gameManager.GetComponent<APISystem>().SetGameOverScreen(gameOverScreen);
        }

        return Task.CompletedTask;
    }

    protected override Task UnloadInternal()
    {
        return Task.CompletedTask;
    }
}
