using System;
using System.Collections.Generic;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    public GameObject m_signInPrefab;
    public GameObject m_signUpPrefab;
    public GameObject m_signIntroPrefab;
    private GameObject m_signInInstance;
    private GameObject m_signIntroInstance;
    private GameObject m_signUpInstance;
    private static AuthManager _instance;

    // Firebase Authentication instance.
    protected Firebase.Auth.FirebaseAuth auth;

    // Firebase User keyed by Firebase Auth.
    protected Dictionary<string, Firebase.Auth.FirebaseUser> userByAuth =
      new Dictionary<string, Firebase.Auth.FirebaseUser>();

    // Flag to check if fetch token is in flight.
    private bool fetchingToken = false;

    private Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;

    public static AuthManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;

        DontDestroyOnLoad(this.gameObject);
    }

    // When the app starts, check to make sure that we have
    // the required dependencies to use Firebase, and if not,
    // add them if possible.
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        EventManager.StartListening(EventNames.ON_SHOW_SIGN_IN_CLICKED, OnShowSignInClicked);
        EventManager.StartListening(EventNames.ON_SHOW_SIGN_UP_CLICKED, OnShowSignUpClicked);
        EventManager.StartListening(EventNames.ON_SHOW_SIGN_INTRO_CLICKED, OnShowSignIntroClicked);

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError(
                  "Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    // Handle initialization of the necessary firebase modules:
    public void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase Auth");
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        auth.IdTokenChanged += IdTokenChanged;
        AuthStateChanged(this, null);
    }

    // Track state changes of the auth object.
    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        Debug.Log("AuthStateChanged");
        Firebase.Auth.FirebaseAuth senderAuth = sender as Firebase.Auth.FirebaseAuth;
        Firebase.Auth.FirebaseUser user = null;

        if (senderAuth != null) userByAuth.TryGetValue(senderAuth.App.Name, out user);
        if (senderAuth == auth && senderAuth.CurrentUser != user)
        {
            bool signedIn = user != senderAuth.CurrentUser && senderAuth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
                ShowSignIntro();
            }
            user = senderAuth.CurrentUser;
            userByAuth[senderAuth.App.Name] = user;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.DisplayName);
                DisplayDetailedUserInfo(user, 1);
                SceneManager.LoadScene("MainMenu");
            }
        }
        else
        {
            Debug.Log("Else: senderAuth.CurrentUser = " + senderAuth.CurrentUser + " user = " + user);
            ShowSignIntro();
        }
    }

    // Track ID token changes.
    void IdTokenChanged(object sender, System.EventArgs eventArgs)
    {
        Firebase.Auth.FirebaseAuth senderAuth = sender as Firebase.Auth.FirebaseAuth;
        if (senderAuth == auth && senderAuth.CurrentUser != null && !fetchingToken)
        {
            senderAuth.CurrentUser.TokenAsync(false).ContinueWithOnMainThread(
              task => Debug.Log(String.Format("Token[0:8] = {0}", task.Result.Substring(0, 8))));
        }
    }

    // Display a more detailed view of a FirebaseUser.
    protected void DisplayDetailedUserInfo(Firebase.Auth.FirebaseUser user, int indentLevel)
    {
        string indent = new String(' ', indentLevel * 2);
        DisplayUserInfo(user, indentLevel);
        Debug.Log(String.Format("{0}Anonymous: {1}", indent, user.IsAnonymous));
        Debug.Log(String.Format("{0}Email Verified: {1}", indent, user.IsEmailVerified));
        Debug.Log(String.Format("{0}Phone Number: {1}", indent, user.PhoneNumber));
        var providerDataList = new List<Firebase.Auth.IUserInfo>(user.ProviderData);
        var numberOfProviders = providerDataList.Count;
        if (numberOfProviders > 0)
        {
            for (int i = 0; i < numberOfProviders; ++i)
            {
                Debug.Log(String.Format("{0}Provider Data: {1}", indent, i));
                DisplayUserInfo(providerDataList[i], indentLevel + 2);
            }
        }
    }

    // Display user information.
    protected void DisplayUserInfo(Firebase.Auth.IUserInfo userInfo, int indentLevel)
    {
        string indent = new String(' ', indentLevel * 2);
        var userProperties = new Dictionary<string, string> {
        {"Display Name", userInfo.DisplayName},
        {"Email", userInfo.Email},
        {"Photo URL", userInfo.PhotoUrl != null ? userInfo.PhotoUrl.ToString() : null},
        {"Provider ID", userInfo.ProviderId},
        {"User ID", userInfo.UserId}
      };
        foreach (var property in userProperties)
        {
            if (!String.IsNullOrEmpty(property.Value))
            {
                Debug.Log(String.Format("{0}{1}: {2}", indent, property.Key, property.Value));
            }
        }
    }

    private void OnShowSignUpClicked(string eventName, ActionParams _data)
    {
        print("OnShowSignUpClicked");

        if (m_signInInstance != null)
        {
            m_signInInstance.SetActive(false);
        }

        if (m_signIntroInstance != null)
        {
            m_signIntroInstance.SetActive(false);
        }

        if (m_signUpInstance == null)
        {
            m_signUpInstance = Instantiate(m_signUpPrefab);
        }
        m_signUpInstance.SetActive(true);
    }

    private void OnShowSignInClicked(string eventName, ActionParams _data)
    {
        print("OnShowSignInClicked");
        ShowSignIn();
    }

    private void ShowSignIn()
    {
        print("ShowSignIn");

        if (m_signUpInstance != null)
        {
            m_signUpInstance.SetActive(false);
        }

        if (m_signIntroInstance != null)
        {
            m_signIntroInstance.SetActive(false);
        }

        if (m_signInInstance == null)
        {
            m_signInInstance = Instantiate(m_signInPrefab);
        }
        m_signInInstance.SetActive(true);
    }

    private void OnShowSignIntroClicked(string eventName, ActionParams _data)
    {
        print("OnShowSignIntroClicked");
        ShowSignIntro();
    }

    private void ShowSignIntro()
    {
        if (m_signUpInstance != null)
        {
            m_signUpInstance.SetActive(false);
        }

        if (m_signInInstance != null)
        {
            m_signInInstance.SetActive(false);
        }

        if (m_signIntroInstance == null)
        {
            m_signIntroInstance = Instantiate(m_signIntroPrefab);
        }
        m_signIntroInstance.SetActive(true);
    }

    // Clean up auth state and auth.
    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
            auth = null;
        }
    }

    public void OnSignOut()
    {
        auth.SignOut();
    }

    public string GetMyDisplayName()
    {
        return auth.CurrentUser.DisplayName;
    }
}