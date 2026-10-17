from pathlib import Path
import sys,subprocess,wave,shutil,numpy as np
P=Path(__file__).resolve().parent;ROOT=P.parents[6];sys.path.insert(0,str(ROOT/'output/prologue-film/tools'));import imageio_ffmpeg
for key,name,duration,release in [('ScytheSlash','Single sharp wh_2026-10-17T07_35_55',.65,.10),('ScytheSpin','Seamless loopin_2026-10-17T07_33_35',1.2667,.20)]:
 src=P/(key+'.mp3')
 if not src.exists():shutil.copy2('/Users/vic/Downloads/ElevenLabs_audio_elevenlabs-sound-effects-v2_'+name+'.mp3',src)
 a=np.frombuffer(subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-v','error','-i',str(src),'-ar','44100','-ac','2','-f','f32le','-'],capture_output=True,check=True).stdout,'<f4').reshape(-1,2).copy()[:round(duration*44100)]
 a[:132]*=np.linspace(0,1,132)[:,None];n=round(release*44100);a[-n:]*=np.linspace(1,0,n)[:,None]
 a*=min(10**(-22/20)/np.sqrt(np.mean(a*a)),10**(-5/20)/abs(a).max())
 with wave.open(str(ROOT/'Assets/Resources/Audio/Skills'/(key+'.wav')),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(44100);w.writeframes(np.round(a*32767).astype('<i2').tobytes())
 print(key,len(a)/44100,float(abs(a).max()))
