from pathlib import Path
import sys,subprocess,wave,json,numpy as np
P=Path(__file__).resolve().parent;ROOT=P.parents[5];SR=44100
sys.path.insert(0,str(ROOT/'output/prologue-film/tools'));import imageio_ffmpeg
raw=subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-v','error','-i',str(P/'take-1.mp3'),'-ar',str(SR),'-ac','2','-f','f32le','-'],capture_output=True,check=True).stdout
a=np.frombuffer(raw,'<f4').reshape(-1,2).astype(float)[:round(2*SR)]
# Gentle onset and decay; retain the generated phrase's tempo and pitch.
a[:round(.025*SR)]*=np.linspace(0,1,round(.025*SR))[:,None]
a[-round(.35*SR):]*=np.linspace(1,0,round(.35*SR))[:,None]**1.5
gain=min(10**(-25/20)/max(np.sqrt(np.mean(a*a)),1e-9),10**(-8/20)/max(abs(a).max(),1e-9));a*=gain
out=ROOT/'Assets/Audio/Sounds/Death.wav'
with wave.open(str(out),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes(np.round(a*32767).astype('<i2').tobytes())
assert abs(a).max()<1 and len(a)/SR+.25<2.7
(P/'validation.json').write_text(json.dumps({'duration':len(a)/SR,'peak_dbfs':float(20*np.log10(abs(a).max())),'rms_dbfs':float(20*np.log10(np.sqrt(np.mean(a*a)))),'selected_take':1,'existing_failure_delay':2.7,'adds_failure_delay':False,'runtime_audition':False},indent=2))
print('Rendered',len(a)/SR,'seconds; no clipping; fits existing 2.7s failure delay.')
