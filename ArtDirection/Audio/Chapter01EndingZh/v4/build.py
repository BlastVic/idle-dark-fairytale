from pathlib import Path
import shutil,json,wave,sys
import numpy as np
P=Path('/Users/vic/Workshop/idle-dark-fairytale/ArtDirection/Audio/Chapter01EndingZh');out=P/'v4';out.mkdir(exist_ok=True)
for pattern in ['page*.wav','page*.mp3','page*.srt','page*-timings.json','page*-script.txt','page*-independent-asr.json']:
 for f in (P/'v3').glob(pattern):shutil.copy2(f,out/f.name)
SR=44100
# Remove only the centers of measured quiet gaps, retaining the phoneme transitions.
cuts5=[(8.66,8.99),(9.47,9.75),(10.40,10.90),(11.49,11.61)]
e=json.loads((P/'v3/quote-edit.json').read_text());shift=e['old_cut'][0]-e['quote_from_page05'][0]
cuts={5:cuts5,2:[(a+shift,b+shift) for a,b in cuts5]}
def remap(t,regions):return t-sum(max(0,min(t,b)-a) for a,b in regions if t>a)
for i,regions in cuts.items():
 with wave.open(str(P/'v3'/f'page{i:02}.wav')) as w:a=np.frombuffer(w.readframes(w.getnframes()),'<i2').copy()
 spans=[];pos=0
 for start,end in regions:
  left,right=round(start*SR),round(end*SR);spans.append(a[pos:left]);pos=right
 spans.append(a[pos:]);result=np.concatenate(spans)
 with wave.open(str(out/f'page{i:02}.wav'),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(result.tobytes())
 meta=json.loads((out/f'page{i:02}-timings.json').read_text());meta['duration']=len(result)/SR
 for r in meta['sentences']:r['start']=round(remap(r['start'],regions),3);r['end']=round(remap(r['end'],regions),3)
 (out/f'page{i:02}-timings.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2))
(out/'quote-continuity-edit.json').write_text(json.dumps(dict(source_version='v3',cuts=cuts,seconds_removed_per_occurrence=sum(b-a for a,b in cuts5),note='仅移除四处低能量停顿中部，保留音节、重音和原声调。逗号附近保留约 0.14 秒停顿，其余约 0.08 秒。'),ensure_ascii=False,indent=2))
sys.path.insert(0,str(P.parents[2]/'output/prologue-film/tools'));import imageio_ffmpeg,subprocess
ff=imageio_ffmpeg.get_ffmpeg_exe()
for i in [2,5]:subprocess.run([ff,'-v','error','-y','-i',str(out/f'page{i:02}.wav'),'-c:a','libmp3lame','-b:a','192k',str(out/f'page{i:02}.mp3')],check=True)
with wave.open(str(out/'chapter01-ending-full.wav'),'wb') as w:
 w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR)
 for i in range(1,6):
  with wave.open(str(out/f'page{i:02}.wav')) as f:w.writeframes(f.readframes(f.getnframes()))
subprocess.run([ff,'-v','error','-y','-i',str(out/'chapter01-ending-full.wav'),'-c:a','libmp3lame','-b:a','192k',str(out/'chapter01-ending-full.mp3')],check=True)
with wave.open(str(out/'page05.wav')) as w:a=w.readframes(w.getnframes())
start=e['quote_from_page05'][0];end=e['quote_from_page05'][1]-sum(b-a for a,b in cuts5)
with wave.open(str(out/'quote-preview.wav'),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(a[round(start*SR)*2:round(end*SR)*2])
for name in ['check_transcription.py','align_subtitles.py']:shutil.copy2(P/'v3'/name,out/name)
f=out/'check_transcription.py';f.write_text(f.read_text().replace('for i in [2,4,5]:','for i in [2,5]:'))
print('Built v4; reduced each quotation by',sum(b-a for a,b in cuts5),'seconds')
