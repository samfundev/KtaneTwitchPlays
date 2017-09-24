using Newtonsoft.Json;
using System.Collections;
using UnityEngine;
using System.IO;

public class TwitchPlaysService : MonoBehaviour
{
    public class ModSettingsJSON
    {
        public string authToken;
        public string userName;
        public string channelName;
        public string serverName;
        public int serverPort;
        public bool debug = false;
        public bool shortUrls = false;
    }

    public BombMessageResponder bombMessageResponder = null;
    public PostGameMessageResponder postGameMessageResponder = null;
    public MissionMessageResponder missionMessageResponder = null;
    public MiscellaneousMessageResponder miscellaneousMessageResponder = null;

    private KMGameInfo _gameInfo = null;
    private KMModSettings _modSettings = null;
    private IRCConnection _ircConnection = null;
    private CoroutineQueue _coroutineQueue = null;
    private CoroutineCanceller _coroutineCanceller = null;

    private MessageResponder _activeMessageResponder = null;
    private Leaderboard _leaderboard = null;

    public static bool DebugMode = false;
    public static LogUploader logUploader = null;
    public static UrlHelper urlHelper = null;

    private void Start()
    {
        _gameInfo = GetComponent<KMGameInfo>();
        _gameInfo.OnStateChange += OnStateChange;

        _modSettings = GetComponent<KMModSettings>();

        ModSettingsJSON settings = JsonConvert.DeserializeObject<ModSettingsJSON>(_modSettings.Settings);
        if (settings == null)
        {
            Debug.LogError("[TwitchPlays] Failed to read connection settings from mod settings.");
            return;
        }

        DebugMode = (settings.debug == true);

        _ircConnection = new IRCConnection(settings.authToken, settings.userName, settings.channelName, settings.serverName, settings.serverPort);
        _ircConnection.Connect();

        _coroutineCanceller = new CoroutineCanceller();

        _coroutineQueue = GetComponent<CoroutineQueue>();
        _coroutineQueue.coroutineCanceller = _coroutineCanceller;

        logUploader = GetComponent<LogUploader>();
        logUploader.ircConnection = _ircConnection;

        urlHelper = GetComponent<UrlHelper>();
        urlHelper.ChangeMode(settings.shortUrls == true);

        _leaderboard = new Leaderboard();
        _leaderboard.LoadDataFromFile();

        SetupResponder(bombMessageResponder);
        SetupResponder(postGameMessageResponder);
        SetupResponder(missionMessageResponder);
        SetupResponder(miscellaneousMessageResponder);

        bombMessageResponder.leaderboard = _leaderboard;
        postGameMessageResponder.leaderboard = _leaderboard;
        miscellaneousMessageResponder.leaderboard = _leaderboard;

        bombMessageResponder.parentService = this;
    }

    private void Update()
    {
        if (_ircConnection != null)
        {
            _ircConnection.Update();
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            InputInterceptor.EnableInput();
        }
    }

    private void OnDestroy()
    {
        if (_ircConnection != null)
        {
            _ircConnection.Disconnect();
        }
    }

    private void OnStateChange(KMGameInfo.State state)
    {
        if (_ircConnection == null)
        {
            return;
        }

        StartCoroutine(StopEveryCoroutine());

        if (_activeMessageResponder != null)
        {
            _activeMessageResponder.gameObject.SetActive(false);
        }

        _activeMessageResponder = GetActiveResponder(state);

        if (_activeMessageResponder != null)
        {
            _activeMessageResponder.gameObject.SetActive(true);
        }        
    }

    private IEnumerator StopEveryCoroutine()
    {
        yield return new WaitForSeconds(2.0f);

        _coroutineQueue.StopQueue();
        _coroutineQueue.CancelFutureSubcoroutines();
        StopAllCoroutines();
    }

    private void SetupResponder(MessageResponder responder)
    {
        if (responder != null)
        {
            responder.SetupResponder(_ircConnection, _coroutineQueue, _coroutineCanceller);
        }
    }

    private MessageResponder GetActiveResponder(KMGameInfo.State state)
    {
        switch (state)
        {
            case KMGameInfo.State.Gameplay:
                return bombMessageResponder;

            case KMGameInfo.State.Setup:
                return missionMessageResponder;

            case KMGameInfo.State.PostGame:
                return postGameMessageResponder;

            default:
                return null;
        }
    }
}
