% % draw data from file

close all
clear
[Vx, name] = xlsread('demo.xlsx',1,'B1:B33');%read everyone's threshold data and their names
Vx(isnan(Vx))=0;%turns unreadable elements into 0

Pts = xlsread('stimulationpoints32.xlsx');%read line number of stimulation points
Hnd = xlsread('handoutline.xlsx');%read point cloud coordinates
Num = size(Hnd,1);%total point number of the cloud
Ptn = 32;%size(Pts,1);%total point number of stimulation points

a1 = Hnd(:,1);
a2 = Hnd(:,2);%the coordinate of hand shape

x = zeros(Ptn,1);
y = zeros(Ptn,1);

for i = 1:Ptn
x(i) = Hnd(Pts(i),1);
y(i) = Hnd(Pts(i),2);%acquire coordinate of stimulation points
end

% Fs = 1e3;%sampling rate
% width = 40;%0.04s per block
% S = size(Vx);
% row = S(1);%total number of samples(16)
% block = ceil(row/width);
% Data = [block,Ptn];
% Map = cell(block,1);

%% spliting blocks for every channel

% for i = 1:Ptn
%     for j = 1:block
%         m = (j-1)*width+1;%block start point
%         n = min(j*width,row);%block end point
%         Data(j,i) = max(Vx(m:n,i))*1000;%extract max of absolute value
%     end
% end

%% map the peaks

%figure
colorbar;
% r = (1:((0.302-1)/40):0.302);
% g = (1:((0.561-1)/40):0.561);
% b = (1:((0.561-1)/40):0.561);
% map = [r;g;b]';
colormap('Jet');
caxis('manual');
caxis([0 3.5]);
cbh = colorbar('Ticks',[0, 1, 2, 3, 3.5],...
         'TickLabels',{'0','1','2','3','3.5'});
cbh.Label.String = 'Current Intensity (mA)';
hold on
% pause(7);

% for k = 1:14
    STM = Vx(:,1);%strength on each point, in a column
    Q = scatteredInterpolant(x,y,STM,'natural');%create a interpolation for each block

    Val = zeros(Num,1);
    for l = 1:Num %here is an L,starting from 1 to Num
        Val(l) = Q(a1(l),a2(l));%interporate a value (color) for every point
    end
    scatter(a1,a2,90,Val,'.');
    
    view(180,90);
    axis off;
    set(gca,'Position',[.10 .05 .60 .90]);%set the position of the figure
    set(gca,'xdir','reverse');%flip the figure
    drawnow
    pause(0.05);
    filename = [name{1,1},'_AA','.tif'];
    saveas(gcf,filename);
% end
