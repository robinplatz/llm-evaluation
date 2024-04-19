using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;
using TMPro;
using System.Diagnostics;

public class Chat : MonoBehaviour
{
    public TextMeshProUGUI textComponent;

    private string GcloudKey;

    // prompt defaults for validation
    // ideal answer: a simple greeting, llm is alive
    readonly string ValidateAlive = @"A chat between a curious user and an artificial intelligence assistant. The assistant gives helpful, accurate and concise answers to the user's questions.
        USER: Hello, are you there?
        ASSISTANT:";
    
    // ideal answer: 2 instructions from valid coordinates
    readonly string ValidateFull = @"A chat between a curious user and an artificial intelligence assistant. The assistant gives helpful, accurate and concise answers to the user's questions.
        USER: You are in control of a game which functions similar to Final Fantasy Tactics Advance. The game is played on a 10x10 grid. Units can move in a all directions up to 3 spaces at most. Units can attack in cardinal directions 1 space away, for an attack to succeed, X or Y of the origin Unit minus X or Y of the target unit must be either -1 or 1. Units cannot stand on the same tile. The grid information is encoded as a visual matrix. Dot means empty tile, P means player 1 unit, O means player 2 unit. You are player 1 and it's your turn. Your goal is to remove the enemy Units. An Attack Deals 1 Damage to their HP, they are removed at 0HP. You can queue 2 moves that are executed one after the other. Example encoded instructions:
        M:2.3:3.4 meaning M= 'move unit' from coordinates x=2,y=3 towards destination x=3,y=4.
        A:3.4:3.5 meaning A= 'attack target' from coordinates x=3,y=4 towards destination x=3,y=5
        The grid looks like this currently:
        .......... 
        ..........
        .....P....
        ..........
        .P........
        ..........
        .O...O....
        ..........
        ..........
        ..........

        Player 1 Units:
        P(2,5) HP1
        P(6,6) HP1
        Player 2 Units:
        O(2,7) HP1
        Based on the position of the units, formulate your turn with encoded instructions.
        ASSISTANT:";

    // ideal answer: coordinates match - the visual board and the list of units can be understood
    readonly string ValidateBoard = @"A chat between a curious user and an artificial intelligence assistant. The assistant gives helpful, accurate and concise answers to the user's questions.
        ..........
        ..........
        ..........
        ..........
        ..........
        ..O.......
        .OP..O....
        ..O.......
        ..........
        ..........
        Player 1 Units:
        P: (3,7)
        Player 2 Units:
        O: (3,6)
        O: (6,7)
        O: (2,7)
        O: (3,8)

        Can you confirm that the visual board and the list of units match their coordinates.
        ";

    // prompt that is used in the call to the API
    private string prompt;

    // Start is called before the first frame update
    void Start()
    {
        // uncomment if following the proposed gcloud authentication method
        // Authenticate_Gcloud();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // curl reference from https://platform.openai.com/docs/quickstart?context=curl
    public IEnumerator PostRequestRemote_GPT() {
        string url = "https://api.openai.com/v1/chat/completions";
        // set the text component to the prompt
        textComponent.text = prompt;

        string jsonBody = $"{{\"model\": \"gpt-4-turbo\",\"messages\": [{{\"role\": \"system\",\"content\": \"You are a helpful, accurate and concise assistant, skilled in correct instructions for a game played with the user.\"}},{{\"role\": \"user\",\"content\": \"{prompt}\"}}]}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        UnityWebRequest www = new(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + System.IO.File.ReadAllText("openai.env"));

        long time0 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        yield return www.SendWebRequest();

        UnityEngine.Debug.Log(www);

        if (www.result != UnityWebRequest.Result.Success)
        {
            long timeErr = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time0;
            UnityEngine.Debug.Log("Err (" + timeErr + "ms): " + www.error);
            System.IO.File.WriteAllText("Assets/IO/input.txt", "");
            System.IO.File.AppendAllText("Assets/IO/input.txt", timeErr + "\n");
            System.IO.File.AppendAllText("Assets/IO/input.txt", www.error + "\n");
        }
        else
        {
            long time1 = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time0;
            // Handle the server response
            string jsonResponse = www.downloadHandler.text;
            UnityEngine.Debug.Log("Response (" + time1 + "ms): " + jsonResponse);
            // json response will look like this:
            // Response: {
            // "id": "chatcmpl-94TPzXvtvM2Hst93qpMI0zfew0xe2",
            // "object": "chat.completion",
            // "created": 1710853423,
            // "model": "gpt-3.5-turbo-0125",
            // "choices": [
            //     {
            //     "index": 0,
            //     "message": {
            //         "role": "assistant",
            //         "content": "M:2.6:2.5 A:2.6:2.7"
            //     },
            //     "logprobs": null,
            //     "finish_reason": "stop"
            //     }
            // ],
            // "usage": {
            //     "prompt_tokens": 388,
            //     "completion_tokens": 18,
            //     "total_tokens": 406
            // },
            // "system_fingerprint": "fp_4f2ebda25a"
            // }

            // Deserialize the JSON response and access the message content
            ChatResponse chatResponse = JsonUtility.FromJson<ChatResponse>(jsonResponse);
            string result = chatResponse.choices[0].message.content;
            textComponent.text += "\n Response: \n";
            // clear the Assets/IO/input.txt
            System.IO.File.WriteAllText("Assets/IO/input.txt", "");
            System.IO.File.AppendAllText("Assets/IO/input.txt", time1 + "\n");
            // Access the results.text property
            UnityEngine.Debug.Log("Result Text: " + result);
            // add the result to the text component
            textComponent.text += "\n" + result;
            // add the result to the Assets/IO/input.txt
            System.IO.File.AppendAllText("Assets/IO/input.txt", result + "\n");
        }
    }

    // curl reference https://docs.aleph-alpha.com/api/complete/
    public IEnumerator PostRequestRemote_AA() {
        string url = "https://api.aleph-alpha.com/complete";
        // set the text component to the prompt
        textComponent.text = prompt;

        string jsonBody = $"{{\"model\": \"luminous-base\",\"prompt\": \"{prompt}\",\"maximum_tokens\": 20}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        UnityWebRequest www = new(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + System.IO.File.ReadAllText("alephalpha.env"));

        long time0 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        yield return www.SendWebRequest();

        UnityEngine.Debug.Log(www);

        if (www.result != UnityWebRequest.Result.Success)
        {
            long timeErr = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time0;
            UnityEngine.Debug.Log("Err (" + timeErr + "ms): " + www.error);
            System.IO.File.WriteAllText("Assets/IO/input.txt", "");
            System.IO.File.AppendAllText("Assets/IO/input.txt", timeErr + "\n");
            System.IO.File.AppendAllText("Assets/IO/input.txt", www.error + "\n");
        }
        else
        {
            long time1 = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time0;
            // Handle the server response
            string jsonResponse = www.downloadHandler.text;
            UnityEngine.Debug.Log("Response (" + time1 + "ms): " + jsonResponse);
            // json response will look like this:
            // Response: {"completions":[{"completion":"\nM:2.3:3.4\nA:3.4:3.5","finish_reason":"maximum_tokens"}],"model_version":"2022-04","optimized_prompt":[{"type":"text","data":"You are in control of a game which functions similar to Final Fantasy Tactics Advance. The game is played on a 10x10 grid. Units may move freely in all directions up to 3 spaces at most.\nUnits can also attack in all cardinal directions up to 1 space away. Units cannot stand on the same tile.\nThe grid information is encoded as a visual matrix. Dot means an empty tile, P means player 1 unit, O means player 2 unit.\nYou are player 1, and it's your turn. Your goal is to remove the enemy Units. An Attack Deals 1 Damage to their HP, they are removed at 0HP.\nYou can queue 2 moves that are executed one after the other. Example Moves:\nM:2.3:3.4 meaning M = 'move unit' from coordinates x = 2, y = 3 towards destination x = 3, y = 4.\nA:3.4:3.5 meaning A = 'attack target' from coordinates x = 3, y = 4 towards destination x = 3, y = 5.\nThe grid looks like this currently:\n.........O\n.........P\n..........\n..........\n..........\nP.........\n..........\n.....O....\n..P.......\n....O.....\nPlayer 1 Units:\nP: (1,9) HP1\nP: (8,2) HP1\nP: (5,0) HP1\nPlayer 2 Units:\nO: (7,5) HP1\nO: (0,9) HP1\nO: (9,4) HP1\nYou are player 2 and it's your turn. Question: What is your encoded instruction?\nAnswer:","add_prefix_space":true}],"num_tokens_prompt_total":352,"num_tokens_generated":20}
            ResponseData responseData = JsonUtility.FromJson<ResponseData>(jsonResponse);

            string result = responseData.completions[0].completion;
            textComponent.text += "\n Response: \n";
            // clear the Assets/IO/input.txt
            System.IO.File.WriteAllText("Assets/IO/input.txt", "");
            System.IO.File.AppendAllText("Assets/IO/input.txt", time1 + "\n");
            // Access the results.text property
            UnityEngine.Debug.Log("Result Text: " + result);
            // add the result to the text component
            textComponent.text += "\n" + result;
            // add the result to the Assets/IO/input.txt
            System.IO.File.AppendAllText("Assets/IO/input.txt", result + "\n");
        }
    }

    // curl reference https://cloud.google.com/vertex-ai/generative-ai/docs/start/quickstarts/quickstart-text?hl=en#generative-ai-test-text-prompt-drest
    public IEnumerator PostRequestRemote_Google() {
        // the request should be structured like this:
        // {
        //     "contents": {
        //         "role": "user",
        //         "parts": {
        //             "text": "$prompt"
        //         }
        //     },
        //     "generation_config": {
        //         "temperature": 0.2,
        //         "topP": 0.8,
        //         "topK": 40
        //     }
        // }
        // POST https://us-central1-aiplatform.googleapis.com/v1/projects/thesis-llm/locations/us-central1/publishers/google/models/gemini-1.0-pro:streamGenerateContent
        // curl -X POST \
        // -H "Authorization: Bearer $(gcloud auth print-access-token)" \
        // -H "Content-Type: application/json; charset=utf-8" \
        // -d @request.json \
        // "https://us-central1-aiplatform.googleapis.com/v1/projects/thesis-llm/locations/us-central1/publishers/google/models/gemini-1.0-pro:streamGenerateContent
        // this needs Authenticate_Gcloud() to be called at Start()

        
        string url = "https://us-central1-aiplatform.googleapis.com/v1/projects/thesis-llm/locations/us-central1/publishers/google/models/gemini-1.0-pro:streamGenerateContent";
        // set the text component to the prompt
        textComponent.text = prompt;

        string jsonBody = $"{{\"contents\": {{\"role\": \"user\",\"parts\": {{\"text\": \"{prompt}\"}}}},\"generation_config\": {{\"temperature\": 0.2,\"topP\": 0.8,\"topK\": 40}}}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        UnityWebRequest www = new(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
        www.SetRequestHeader("Authorization", "Bearer " + GcloudKey);

        long time0 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        yield return www.SendWebRequest();

        UnityEngine.Debug.Log(www);

        if (www.result != UnityWebRequest.Result.Success)
        {
            long timeErr = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time0;
            UnityEngine.Debug.Log("Err (" + timeErr + "ms): " + www.error);
            System.IO.File.WriteAllText("Assets/IO/input.txt", "");
            System.IO.File.AppendAllText("Assets/IO/input.txt", timeErr + "\n");
            System.IO.File.AppendAllText("Assets/IO/input.txt", www.error + "\n");
        }
        else
        {
            long time1 = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time0;
            // Handle the server response
            string jsonResponse = www.downloadHandler.text;
            UnityEngine.Debug.Log("Response (" + time1 + "ms): " + jsonResponse);
            // json response will look like this:
            // {
            //     "candidates": [
            //         {
            //             "content": {
            //                 "role": "model",
            //                 "parts": [
            //                     {
            //                         "text": "Ingredients:\n\n- 3 ripe bananas, mashed\n- 1 cup sugar"
            //                     }
            //                 ]
            //             }
            //         }
            //     ]
            // }
            // Deserialize the JSON response and access the message content
            ChatResponse chatResponse = JsonUtility.FromJson<ChatResponse>(jsonResponse);
            string result = chatResponse.choices[0].message.content;
            textComponent.text += "\n Response: \n";
            // clear the Assets/IO/input.txt
            System.IO.File.WriteAllText("Assets/IO/input.txt", "");
            System.IO.File.AppendAllText("Assets/IO/input.txt", time1 + "\n");
            // Access the results.text property
            UnityEngine.Debug.Log("Result Text: " + result);
            // add the result to the text component
            textComponent.text += "\n" + result;
            // add the result to the Assets/IO/input.txt
            System.IO.File.AppendAllText("Assets/IO/input.txt", result + "\n");
        }
    }

    public void Authenticate_Gcloud()
    {
        // a bunch of hardcoded gcloud stuff, REPLACE if you are rebuilding the project
        // too much changes depending on OS and your preferred authentication method so I'm not providing a general solution here
        // this assumes your key.json is in the main project dir, next to the openai.env and alephalpha.env

        // follows documentation from:
        // https://cloud.google.com/sdk/gcloud/reference/auth/login
        // https://cloud.google.com/sdk/gcloud/reference/auth/activate-service-account?cloudshell=false

        string keyFile = Application.dataPath + "/../thesis-llm-8a37048b035f.json";
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/C \"C:\\Users\\robin\\AppData\\Local\\Google\\Cloud SDK\\google-cloud-sdk\\bin\\gcloud.cmd\" auth activate-service-account unity-runner@thesis-llm.iam.gserviceaccount.com --key-file=" + keyFile + " --project=thesis-llm",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            UnityEngine.Debug.Log("gcloud Authentication successful");
            // set the GcloudKey using cmd.exe "gcloud auth print-access-token"
            startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C \"C:\\Users\\robin\\AppData\\Local\\Google\\Cloud SDK\\google-cloud-sdk\\bin\\gcloud.cmd\" auth print-access-token",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process = new Process { StartInfo = startInfo };
            process.Start();
            GcloudKey = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            // UnityEngine.Debug.Log("GcloudKey: " + GcloudKey);
        }
        else
        {
            UnityEngine.Debug.LogError("gcloud Authentication failed: " + JsonUtility.ToJson(output));
            UnityEngine.Debug.LogError("Error details: " + error);
        }
    }

    public void ClickInferLocal() {
        ClickInfer(true, null);
    }

    public void ClickInferRemote_GPT() {
        ClickInfer(false, "gpt");
    }

    public void ClickInferRemote_AA() {
        ClickInfer(false, "alephalpha");
    }

    public void ClickInferRemote_Google() {
        ClickInfer(false, "google");
    }

    // primes the prompt to be read, prints Debugging information of exact sent prompt
    private void ClickInfer(bool local, string provider) {
        // read prompt from Assets/IO/output.txt
        prompt = EscapeString(System.IO.File.ReadAllText("Assets/IO/output.txt"));
        // write it to Assets/IO/Debugging.txt
        // create file if not exists
        if (!System.IO.File.Exists("Assets/IO/Debugging.txt"))
        {
            System.IO.File.Create("Assets/IO/Debugging.txt").Dispose();
        }
        System.IO.File.WriteAllText("Assets/IO/Debugging.txt", prompt);
        if (local)
        {
            StartCoroutine(PostRequestLocal());
        } else
        {
            if (provider == "gpt")
            {
                StartCoroutine(PostRequestRemote_GPT());
            } else if (provider == "alephalpha")
            {
                StartCoroutine(PostRequestRemote_AA());
            } else if (provider == "google")
            {
                StartCoroutine(PostRequestRemote_Google());
            }
        }
    }

    public void ClickLocalAlive() {
        prompt = EscapeString(ValidateAlive);
        StartCoroutine(PostRequestLocal());
    }

    public void ClickLocalFull() {
        prompt = EscapeString(ValidateFull);
        StartCoroutine(PostRequestLocal());
    }

    public void ClickLocalBoard() {
        prompt = EscapeString(ValidateBoard);
        StartCoroutine(PostRequestLocal());
    }

    public IEnumerator PostRequestLocal()
    {
        if (prompt == null)
        {
            UnityEngine.Debug.Log("Prompt is null");
            prompt = EscapeString(ValidateAlive);
        }

        string url = "http://172.25.89.116:5000/api/v1/generate";
        // set the text component to the prompt
        textComponent.text = prompt;

        string jsonBody = $"{{\"prompt\": \"{prompt}\",\"max_new_tokens\": 120,\"do_sample\": true,\"temperature\": 1.3,\"top_p\": 0.1,\"typical_p\": 1,\"epsilon_cutoff\": 0,\"eta_cutoff\": 0,\"tfs\": 1,\"top_a\": 0,\"repetition_penalty\": 1.18,\"top_k\": 40,\"min_length\": 0,\"no_repeat_ngram_size\": 0,\"num_beams\": 1,\"penalty_alpha\": 0,\"length_penalty\": 1,\"early_stopping\": false,\"mirostat_mode\": 0,\"mirostat_tau\": 5,\"mirostat_eta\": 0.1,\"seed\": -1,\"add_bos_token\": true,\"truncation_length\": 4096,\"ban_eos_token\": false,\"skip_special_tokens\": true,\"stopping_strings\": []}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        UnityWebRequest www = new(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");

        long time0 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            long timeErr = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time0;
            UnityEngine.Debug.Log("Err (" + timeErr + "ms): " + www.error);
            System.IO.File.WriteAllText("Assets/IO/input.txt", "");
            System.IO.File.AppendAllText("Assets/IO/input.txt", timeErr + "\n");
            System.IO.File.AppendAllText("Assets/IO/input.txt", www.error + "\n");
        }
        else
        {
            long time1 = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time0;
            // Handle the server response
            string jsonResponse = www.downloadHandler.text;
            // UnityEngine.Debug.Log("Response: " + jsonResponse);
            // Parse the JSON response
            var jsonObject = JsonUtility.FromJson<RootObject>(jsonResponse);
            textComponent.text += "\n Response: \n";
            // clear the Assets/IO/input.txt
            System.IO.File.WriteAllText("Assets/IO/input.txt", "");
            System.IO.File.AppendAllText("Assets/IO/input.txt", time1 + "\n");
            // Access the results.text property
            foreach (var result in jsonObject.results)
            {
                UnityEngine.Debug.Log("Response (" + time1 + "ms): " + result.text);
                // add the result to the text component
                textComponent.text += "\n" + result.text;
                // add the result to the Assets/IO/input.txt
                System.IO.File.AppendAllText("Assets/IO/input.txt", result.text + "\n");
            }
        }
    }

    // should be called before any inputs are sent to the local API as control characters and newline etc will crash the json decoder
    string EscapeString(string input)
    {
        var result = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            switch (c)
            {
                case '\"': result.Append("\\\""); break;
                case '\\': result.Append("\\\\"); break;
                case '\0': result.Append("\\0"); break;
                case '\a': result.Append("\\a"); break;
                case '\b': result.Append("\\b"); break;
                case '\f': result.Append("\\f"); break;
                case '\n': result.Append("\\n"); break;
                case '\r': result.Append("\\r"); break;
                case '\t': result.Append("\\t"); break;
                case '\v': result.Append("\\v"); break;
                default:
                    if (char.IsControl(c))
                    {
                        result.Append("\\u" + ((int)c).ToString("x4"));
                    }
                    else
                    {
                        result.Append(c);
                    }
                    break;
            }
        }
        return result.ToString();
    }
}

// local response
[Serializable]
public class Result
{
    public string text;
}

[Serializable]
public class RootObject
{
    public List<Result> results;
}

// openai response
[Serializable]
public class ChatResponse
{
    public string id;
    public string objectName;
    public long created;
    public string model;
    public Choice[] choices;
    public Usage usage;
    public string system_fingerprint;
}

[Serializable]
public class Choice
{
    public int index;
    public Message message;
    public string finish_reason;
}

[Serializable]
public class Message
{
    public string role;
    public string content;
}

[Serializable]
public class Usage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}

// alephalpha response
[Serializable]
public class Completion
{
    public string completion;
    public string finish_reason;
}

[Serializable]
public class OptimizedPrompt
{
    public string type;
    public PromptData data;
}

[Serializable]
public class PromptData
{
    public string text;
}

[Serializable]
public class ResponseData
{
    public Completion[] completions;
    public string model_version;
    public OptimizedPrompt[] optimized_prompt;
    public int num_tokens_prompt_total;
    public int num_tokens_generated;
}

//gcloud response
[Serializable]
public class Candidates
{
    public Content[] content;
}

[Serializable]
public class Content
{
    public string role;
    public Part[] parts;
}

[Serializable]
public class Part
{
    public string text;
}