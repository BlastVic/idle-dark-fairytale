from pathlib import Path
import sys,subprocess,json,numpy as np
P=Path(__file__).resolve().parent;ROOT=P.parents[5];SR=44100
sys.path.insert(0,str(ROOT/'output/prologue-film/tools'));import imageio_ffmpeg
FF=imageio_ffmpeg.get_ffmpeg_exe()
def read(p):return np.frombuffer(subprocess.run([FF,'-v','error','-i',str(p),'-ar',str(SR),'-ac','2','-f','f32le','-'],capture_output=True,check=True).stdout,'<f4').reshape(-1,2).astype(float)
def fade(a,attack,release):
 a=a.copy();n=round(attack*SR);m=round(release*SR);a[:n]*=np.linspace(0,1,n)[:,None];a[-m:]*=np.linspace(1,0,m)[:,None];return a
base=read(P/'approved-opening.mp3')
# Keep the approved wood/mechanical track intact and align the emotional accent to lid release.
a=read(P/'accent-1.mp3');energy=np.max(abs(a),axis=1);onset=np.where(energy>energy.max()*.07)[0][0];a=a[max(0,onset-round(.006*SR)):]
# Pitch-preserving duration adjustment fits the complete accent into the opening's remaining 0.9s.
factor=max(1,len(a)/(SR*.90));filters=[]
while factor>2:filters.append('atempo=2');factor/=2
filters.append('atempo='+str(factor))
b=subprocess.run([FF,'-v','error','-f','f32le','-ar',str(SR),'-ac','2','-i','-','-af',','.join(filters),'-f','f32le','-'],input=a.astype('<f4').tobytes(),capture_output=True,check=True).stdout
a=np.frombuffer(b,'<f4').reshape(-1,2).astype(float)[:round(.90*SR)];a=fade(a,.006,.20);a*=.32/max(abs(a).max(),1e-9)
mix=base.copy();start=round(1.25*SR);n=min(len(a),len(mix)-start);mix[start:start+n]+=a[:n]
# Quiet reverse swell builds anticipation under the last shakes, resolving at lid opening.
swell=a[:round(.60*SR)][::-1].copy();swell=fade(swell,.12,.015);swell*=np.linspace(0,.28,len(swell))[:,None]
j=start-len(swell);mix[j:start]+=swell
assert abs(mix).max()<.99
out=ROOT/'output/chest-opening-celebration/celebration-opening.mp3'
subprocess.run([FF,'-v','error','-y','-f','f32le','-ar',str(SR),'-ac','2','-i','-','-c:a','libmp3lame','-b:a','192k',str(out)],input=mix.astype('<f4').tobytes(),check=True)
check=read(out);assert abs(check).max()<1
(P/'mix-notes.json').write_text(json.dumps({'duration':len(check)/SR,'accent_start':1.25,'swell_start':j/SR,'peak_dbfs':float(20*np.log10(abs(check).max())),'source_onset_trim':onset/SR,'runtime_asset_modified':False,'runtime_audition':False},indent=2))
print('Built comparison variant:',len(check)/SR,'seconds, peak dBFS',20*np.log10(abs(check).max()))
