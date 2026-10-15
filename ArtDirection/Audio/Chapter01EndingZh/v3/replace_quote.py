from pathlib import Path
import json,wave,sys,subprocess
import numpy as np
P=Path(__file__).resolve().parent;sys.path.insert(0,str(P.parents[3]/'output/prologue-film/tools'))
import imageio_ffmpeg
SR=44100

def read(p):
 with wave.open(str(p)) as f:assert f.getframerate()==SR;return np.frombuffer(f.readframes(f.getnframes()),'<i2').astype(float)/32768

def write(p,a):
 with wave.open(str(p),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(SR);f.writeframes(np.round(a*32767).astype('<i2').tobytes())

def quiet(a,lo,hi):
 n=round(.02*SR);best=min(range(round(lo*SR),round(hi*SR),round(.005*SR)),key=lambda i:np.mean(a[i-n//2:i+n//2]**2))
 return best
old=read(P.parent/'v2/page02.wav');new=read(P/'page05.wav')
# Locate edits in inter-sentence gaps, retaining consonants and natural tails.
a=quiet(old,11.55,11.95);b=quiet(old,14.5,14.85)
c=quiet(new,7.6+.45,7.95+.45);d=quiet(new,11.75+.45,12.0+.45)
quote=new[c:d].copy();head=old[:a].copy();tail=old[b:].copy();k=round(.005*SR)
head[-k:]*=np.linspace(1,0,k);quote[:k]*=np.linspace(0,1,k);quote[-k:]*=np.linspace(1,0,k);tail[:k]*=np.linspace(0,1,k)
mix=np.concatenate([head,quote,tail]);write(P/'page02.wav',mix)
meta=json.loads((P/'page02-timings.json').read_text());meta.update(duration=len(mix)/SR,source='v2 page02, replacing only quotation with v3 page05 quotation');(P/'page02-timings.json').write_text(json.dumps(meta,indent=2))
(P/'quote-edit.json').write_text(json.dumps(dict(old_cut=[a/SR,b/SR],quote_from_page05=[c/SR,d/SR],edge_fade_ms=5,note='Same new quote in pages 2 and 5; all remaining page02 narration from v2.'),indent=2))
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-v','error','-y','-i',str(P/'page02.wav'),'-c:a','libmp3lame','-b:a','192k',str(P/'page02.mp3')],check=True)
with wave.open(str(P/'chapter01-ending-full.wav'),'wb') as f:
 f.setnchannels(1);f.setsampwidth(2);f.setframerate(SR)
 for i in range(1,6):
  with wave.open(str(P/f'page{i:02}.wav')) as w:f.writeframes(w.readframes(w.getnframes()))
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-v','error','-y','-i',str(P/'chapter01-ending-full.wav'),'-c:a','libmp3lame','-b:a','192k',str(P/'chapter01-ending-full.mp3')],check=True)
print((P/'quote-edit.json').read_text())
