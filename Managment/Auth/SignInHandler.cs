using System.Collections.Generic;
using UnityEngine;
using Firebase.Extensions;
using System;
using System.Threading.Tasks;
using UnityEngine.UI;
using Firebase.Auth;
using TMPro;

public class SignInHandler : MonoBehaviour
{
    public GameObject SignInUIContainer;

    public TMP_InputField emailTextBox;
    public TMP_InputField passwordTextBox;
    public Button signinButton;
    public TMP_Text emailErrorText;
    public TMP_Text passwordErrorText;
    protected Firebase.Auth.FirebaseAuth auth;
    // Whether to sign in / link or reauthentication *and* fetch user profile data.
    protected bool signInAndFetchProfile = true;

    private string m_password;
    private string m_email;

    // Start is called before the first frame update
    void Start()
    {
        passwordTextBox.inputType = TMP_InputField.InputType.Password;
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        SignIn();
    }

    private void SignIn()
    {
        SetUIActiveState(false);
        bool success = TrySignInFromCache();
        if (!success)
        {
            SetUIActiveState(true);
        }
    }

    private bool TrySignInFromCache()
    {
        //return false;

        m_email = PlayerPrefs.GetString("user_email");
        m_password = PlayerPrefs.GetString("user_password");
        if (m_email != "" && m_password != "")
        {
            SigninWithEmailAsync(m_email, m_password);
            return true;
        }
        return false;
    }

    public void OnSignInBtnClicked()
    {
        m_email = emailTextBox.text;
        m_password = passwordTextBox.text;
        SigninWithEmailAsync(m_email, m_password);
    }

    public void OnCancelClicked()
    {
        EventManager.TriggerEvent(EventNames.ON_SHOW_SIGN_INTRO_CLICKED);
    }

    // Sign-in with an email and password.
    public Task SigninWithEmailAsync(string email, string password)
    {
        Debug.Log(String.Format("Attempting to sign in as "+ email));
        DisableUI();
        if (signInAndFetchProfile)
        {
            return auth.SignInAndRetrieveDataWithCredentialAsync(
              Firebase.Auth.EmailAuthProvider.GetCredential(email, password)).ContinueWithOnMainThread(
                HandleSignInWithSignInResult);
        }
        else
        {
            return auth.SignInWithEmailAndPasswordAsync(email, password)
              .ContinueWithOnMainThread(HandleSignInWithUser);
        }
    }

    // Called when a sign-in with profile data completes.
    void HandleSignInWithSignInResult(Task<Firebase.Auth.SignInResult> task)
    {
        EnableUI();
        if (LogTaskCompletion(task, "Sign-in"))
        {
            DisplaySignInResult(task.Result, 1);
        }
    }

    // Display user information reported
    protected void DisplaySignInResult(Firebase.Auth.SignInResult result, int indentLevel)
    {
        string indent = new String(' ', indentLevel * 2);
        var metadata = result.Meta;
        if (metadata != null)
        {
            Debug.Log(String.Format("{0}Created: {1}", indent, metadata.CreationTimestamp));
            Debug.Log(String.Format("{0}Last Sign-in: {1}", indent, metadata.LastSignInTimestamp));
        }
        var info = result.Info;
        if (info != null)
        {
            Debug.Log(String.Format("{0}Additional User Info:", indent));
            Debug.Log(String.Format("{0}  User Name: {1}", indent, info.UserName));
            Debug.Log(String.Format("{0}  Provider ID: {1}", indent, info.ProviderId));
            DisplayProfile<string>(info.Profile, indentLevel + 1);
        }
    }

    // Display additional user profile information.
    protected void DisplayProfile<T>(IDictionary<T, object> profile, int indentLevel)
    {
        string indent = new String(' ', indentLevel * 2);
        foreach (var kv in profile)
        {
            var valueDictionary = kv.Value as IDictionary<object, object>;
            if (valueDictionary != null)
            {
                Debug.Log(String.Format("{0}{1}:", indent, kv.Key));
                DisplayProfile<object>(valueDictionary, indentLevel + 1);
            }
            else
            {
                Debug.Log(String.Format("{0}{1}: {2}", indent, kv.Key, kv.Value));
            }
        }
    }

    void DisableUI()
    {
        emailTextBox.DeactivateInputField();
        passwordTextBox.DeactivateInputField();
        signinButton.interactable = false;
        emailErrorText.enabled = false;
        passwordErrorText.enabled = false;
    }

    void EnableUI()
    {
        emailTextBox.ActivateInputField();
        passwordTextBox.ActivateInputField();
        signinButton.interactable = true;
    }

    // Log the result of the specified task, returning true if the task
    // completed successfully, false otherwise.
    protected bool LogTaskCompletion(Task task, string operation)
    {
        bool complete = false;
        if (task.IsCanceled)
        {
            Debug.Log(operation + " canceled.");
        }
        else if (task.IsFaulted)
        {
            Debug.Log(operation + " encounted an error.");
            foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
            {
                string authErrorCode = "";
                Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                if (firebaseEx != null)
                {
                    authErrorCode = String.Format("AuthError.{0}: ",
                      ((Firebase.Auth.AuthError)firebaseEx.ErrorCode).ToString());
                    GetErrorMessage((Firebase.Auth.AuthError)firebaseEx.ErrorCode);
                }
                Debug.Log(authErrorCode + exception.ToString());
            }
        }
        else if (task.IsCompleted)
        {
            Debug.Log(operation + " completed");
            complete = true;
            PlayerPrefs.SetString("user_email", m_email);
            PlayerPrefs.SetString("user_password", m_password);
        }
        return complete;
    }

    // Called when a sign-in without fetching profile data completes.
    void HandleSignInWithUser(Task<Firebase.Auth.FirebaseUser> task)
    {
        EnableUI();
        if (LogTaskCompletion(task, "Sign-in"))
        {
            
            Debug.Log(String.Format("{0} signed in", task.Result.DisplayName));
        }
    }

    private void GetErrorMessage(AuthError errorCode)
    {
        switch (errorCode)
        {
            case AuthError.MissingPassword:
                passwordErrorText.text = "Missing password.";
                passwordErrorText.enabled = true;
                break;
            case AuthError.WrongPassword:
                passwordErrorText.text = "Incorrect password.";
                passwordErrorText.enabled = true;
                break;
            case AuthError.InvalidEmail:
                emailErrorText.text = "Invalid email.";
                emailErrorText.enabled = true;
                break;
            case AuthError.MissingEmail:
                emailErrorText.text = "Missing email.";
                emailErrorText.enabled = true;
                break;
            case AuthError.UserNotFound:
                emailErrorText.text = "Account not found.";
                emailErrorText.enabled = true;
                break;
            default:
                emailErrorText.text = "Unknown error occurred.";
                emailErrorText.enabled = true;
                break;
        }
    }

    public void SetUIActiveState(bool active)
    {
        SignInUIContainer.SetActive(active);
    }
}