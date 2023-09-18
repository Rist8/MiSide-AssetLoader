using System.Reflection;
using Coffee.UIEffects;
using Dummiesman;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class Plugin : MonoBehaviour
{
	private string currentSceneName;

	private void Start()
	{
		ReadAssetsConfig();
		LoadAssetsForPatch();
	}

	private static Dictionary<string, Assimp.Mesh[]> loadedModels;
	private static Dictionary<string, Texture2D> loadedTextures;
	private static Dictionary<string, AudioClip> loadedAudio;
	private static List<(string name, string[] args)> assetCommands;

	public GameObject mita;

	void ReadAssetsConfig()
	{
		string filePath = PluginInfo.AssetsFolder + "/assets_config.txt";
		assetCommands = new();
        
        try
        {
            using (StreamReader sr = new StreamReader(filePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    // Ignore empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                        continue;
                    
                    // Split line on commands with arguments list
                    string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    assetCommands.Add((parts[0], parts.Skip(1).ToArray()));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
	}

	void LoadAssetsForPatch()
	{
		if (loadedModels != null) return;

		loadedModels = new();
		loadedTextures = new();
		loadedAudio = new();
		
		// audio
		string[] files = AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "ogg");
		foreach (var file in files)
		{
			var audioFile = AssetLoader.LoadAudio(file);
			var filename = Path.GetFileNameWithoutExtension(file);
			loadedAudio.Add(filename, audioFile);
			PluginInfo.Instance.Logger.LogInfo($"Loaded audio from file: '{filename}'");
		}

		// meshes
		files = AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "fbx");
		foreach (var file in files)
		{
			var meshes = AssetLoader.LoadFBX(file);
			var filename = Path.GetFileNameWithoutExtension(file);
			loadedModels.Add(filename, meshes);
			PluginInfo.Instance.Logger.LogInfo($"Loaded meshes from file: '{filename}', {meshes.Length} meshes");
		}
		
		/*files = 
    		Directory.GetFiles(PluginInfo.AssetsFolder, "*.obj", SearchOption.AllDirectories);
		foreach (var file in files)
		{
			var loader = new OBJLoader();
			loader.Load(file);
			var meshes = loader.Builders.Values.Select(builder => builder.Build()).ToArray();
			var filename = Path.GetFileNameWithoutExtension(file);
			loadedModels.Add(filename, meshes);
			PluginInfo.Instance.Logger.LogInfo($"Loaded meshes from file: '{filename}', {loader.Builders.Count} meshes");
		}*/

		// textures
		files = AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "png", "jpg", "jpeg");

		foreach (var file in files)
		{
			Texture2D texture = AssetLoader.LoadTexture(file);
			if (texture != null)
			{
				var filename = Path.GetFileNameWithoutExtension(file);
				loadedTextures.Add(filename, texture);
				PluginInfo.Instance.Logger.LogInfo($"Loaded texture from file: '{filename}' " + loadedTextures[filename]);
			}
		}
	}

    void FindMita()
    {
		var animators = Reflection.FindObjectsOfType<Animator>(true);
		GameObject mitaAnimator = null;
		foreach (var obj in animators)
		{
			var anim = obj.Cast<Animator>();
			if (anim.runtimeAnimatorController != null && anim.runtimeAnimatorController.name == "Mita")
			{
				mitaAnimator = anim.gameObject;	break;
			}
		}
		
		if (mitaAnimator == null)
		{
			Debug.Log("Found no animators to patch");
			mita = null;
		}
		else
		{
			Debug.Log("Found Mita");
			mita = mitaAnimator;
			PatchMita();
		}
	}

	void PatchMita()
	{
		return;
		Debug.Log("Patching Mita");
		var renderersList = Reflection.GetComponentsInChildren<SkinnedMeshRenderer>(mita);
		var renderers = new Dictionary<string, Renderer>();
		foreach (var renderer in renderersList)
			renderers[renderer.name.Trim()] = renderer;

		foreach (var command in assetCommands)
		{
			if (command.args.Length == 0 || command.args[0] != "Mita") continue;

			try
			{
				if (command.name == "remove")
				{
					renderers[command.args[1]].gameObject.SetActive(false);
				}
				else if (command.name == "replace_tex")
				{
					renderers[command.args[1]].material.mainTexture = loadedTextures[command.args[2]];
					if (command.args.Length >= 4 && ColorUtility.TryParseHtmlString(command.args[3], out Color color))
						renderers[command.args[1]].material.color = color;
				}
				else if (command.name == "replace_mesh")
				{
					var meshData = loadedModels[command.args[2]].First(mesh => 
						mesh.Name == (command.args.Length >= 4 ? command.args[3] : command.args[2]));
					
					if (renderers[command.args[1]] is SkinnedMeshRenderer sk)
						sk.sharedMesh = AssetLoader.BuildMesh(meshData, new AssetLoader.ArmatureData(sk));
					else
						renderers[command.args[1]].GetComponent<MeshFilter>().mesh = AssetLoader.BuildMesh(meshData);
				}
				else if (command.name == "create_skinned_appendix")
				{
					var parent = renderers[command.args[2]];

					SkinnedMeshRenderer obj = UnityEngine.Object.Instantiate(
						parent,
						parent.transform.position,
						parent.transform.rotation,
						parent.transform.parent).Cast<SkinnedMeshRenderer>();
					obj.name = command.args[1];
					obj.material = new Material(parent.material);
					obj.gameObject.SetActive(true);
					renderers[command.args[1]] = obj;
					Debug.Log("Added skinned appendix " + obj.name);
				}
				else if (command.name == "create_static_appendix")
				{
					MeshRenderer obj = new GameObject().AddComponent<MeshRenderer>();
					obj.name = command.args[1];
					obj.material = new Material(mita.transform.Find("Attribute").GetComponent<SkinnedMeshRenderer>().material);
					obj.gameObject.AddComponent<MeshFilter>();
					obj.transform.parent = RecursiveFindChild(mita.transform.Find("Armature"), command.args[2]);
					obj.transform.localPosition = Vector3.zero;
					obj.transform.localScale = Vector3.one;
					obj.transform.localEulerAngles = new Vector3(-90f, 0, 0);
					obj.gameObject.SetActive(true);
					renderers[command.args[1]] = obj;
					Debug.Log("Added static appendix " + obj.name);
				}
			} catch (Exception e)
			{
				Debug.LogError("Error while processing command " + command.name + " " + string.Join(' ', command.args) + "\n" + e.ToString());
			}
		}
		
		Debug.Log("Patching Mita completed");
    }

	Transform RecursiveFindChild(Transform parent, string childName)
	{
		for (int i = 0; i < parent.childCount; i++)
		{
			var child = parent.GetChild(i);
			if(child.name == childName) return child;
			else {
				Transform found = RecursiveFindChild(child, childName);
				if (found != null) return found;
			}
		}
		return null;
	}

	VideoPlayer currentVideoPlayer = null;
	Action onCurrentVideoEnded = null;
	Image logo = null;

	void PatchMenuScene()
	{
		Debug.Log("Patching game scene");

		var command = assetCommands.FirstOrDefault<(string? name, string[]? args)>(item => item.name == "menu_logo", (null, null));
		if (command.name != null)
		{
			var animators = Reflection.FindObjectsOfType<Animator>(true);
			GameObject gameName = null;
			foreach (var obj in animators) 
				if (obj.name == "NameGame")
				{ 
					gameName = obj.Cast<Animator>().gameObject; 
					Destroy(obj);
					break;
				}

			for (int i = 0; i < gameName.transform.childCount; i++)
			{
				var tr = gameName.transform.GetChild(i);
				if (tr.name == "Background")
				{
					Texture2D tex = loadedTextures[command.args[0]];
					Destroy(Reflection.GetComponent<UIShiny>(tr));

					logo = Reflection.GetComponent<Image>(tr);
					logo.preserveAspect = true;
					logo.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2.0f);
					Reflection.GetComponent<RectTransform>(tr).sizeDelta = new Vector2(1600, 400);
				}
				else
					tr.gameObject.SetActive(false);
			}
		}
		
		command = assetCommands.FirstOrDefault<(string? name, string[]? args)>(item => item.name == "menu_music", (null, null));
		if (command.name != null)
		{
			var musicSources = Reflection.FindObjectsOfType<AudioSource>(true);
			foreach (var source in musicSources)
				if (source.name == "Music")
				{
					source.clip = loadedAudio[command.args[0]];
					source.volume = 1;
					source.Play();
					break;
				}
		}

		ClothesMenuPatcher.Run(mita);

		Debug.Log("Patching completed");
	}
	void Update()
	{
		if (currentSceneName != SceneManager.GetActiveScene().name)
		{
			currentSceneName = SceneManager.GetActiveScene().name;
			OnSceneChanged();
		}

		if (currentVideoPlayer != null)
		{
			if ((ulong) currentVideoPlayer.frame + 5 > currentVideoPlayer.frameCount)
			{
				Debug.Log("Video ended");
				Destroy(currentVideoPlayer.transform.parent.gameObject);
				currentVideoPlayer = null;
				onCurrentVideoEnded?.Invoke();
				onCurrentVideoEnded = null;
			}
		}
		if (currentSceneName == "SceneMenu")
		{
			if (logo != null)
				logo.color = Color.white;
		}
	}

	void OnSceneChanged()
	{
		try
		{
			Debug.Log("Scene changed to " + currentSceneName);
			FindMita();
			if (currentSceneName == "SceneMenu") PatchMenuScene();
		} catch (Exception e){
			Debug.Log(e.ToString());
			enabled = false;
		}
	}

	void PlayFullscreenVideo(Action onVideoEnd)
	{
		var rootGO = SceneManager.GetActiveScene().GetRootGameObjects();
		foreach (var go in rootGO)
			if (go != gameObject) go.gameObject.SetActive(false);

		Camera camera = new GameObject("VideoCamera").AddComponent<Camera>();
		VideoPlayer videoPlayer = new GameObject("VideoPlayer").AddComponent<VideoPlayer>();
		videoPlayer.transform.parent = camera.transform;
		camera.backgroundColor = Color.black;
		camera.gameObject.AddComponent<AudioListener>();
		videoPlayer.playOnAwake = false;
		videoPlayer.targetCamera = camera;
		videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
		videoPlayer.url = "file://" + PluginInfo.AssetsFolder + "/intro.mp4";
		videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
		videoPlayer.SetTargetAudioSource(0, videoPlayer.gameObject.AddComponent<AudioSource>());
		
		currentVideoPlayer = videoPlayer;
		onCurrentVideoEnded = onVideoEnd;

		videoPlayer.Play();
		Debug.Log("Video started");
	}
}
