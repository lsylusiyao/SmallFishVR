function result = ShowDirSpeed(path)
% ��VR�ֱ����ݽ���Ϊ0123֮��ĺ���
    data = load(path);
    divisionPoint = [15,25,40,55];
    dataColor = data(:,4);
    dataSpeed = data(:,6);
    dataDirection = ones(length(dataSpeed),1);
    dataLR = data(:,7);
    dataFB = data(:,8);
    
    dataColor(dataColor >= divisionPoint(1)) = -1;
    dataColor(dataColor <= -divisionPoint(1)) = 1;
    dataColor(dataColor < divisionPoint(1) & dataColor > -divisionPoint(1)) = 0;
    
    dataSpeed(dataSpeed >= -divisionPoint(1)) = 0;
    dataSpeed(dataSpeed < -divisionPoint(1) & dataSpeed >= -divisionPoint(2)) = 1;
    dataSpeed(dataSpeed < -divisionPoint(2) & dataSpeed >= -divisionPoint(3)) = 2;
    dataSpeed(dataSpeed < -divisionPoint(3) & dataSpeed >= -divisionPoint(4)) = 3;
    dataSpeed(dataSpeed < -divisionPoint(4)) = 4;
    
    % ֹͣ��0�� ǰ����1�� ����2�� ����3
    dataDirection(abs(dataLR) < 10 & abs(dataFB) < 10) = 0;
    dataDirection(~(abs(dataLR) < 10 & abs(dataFB) < 10) & abs(dataLR) >= abs(dataFB)) = 2; % �ҵ��������Ҳ���
    dataDirection(dataDirection == 2 & dataLR > 0) = 3;
    dataDirection(dataDirection == 1 & dataFB <= 0) = 0;
    % ʣ�µĶ���ǰ����
    
    figure;
    plot(dataColor);
    xlabel('ʱ��');
    ylabel('��ɫ�л�����');
    title('��ɫ�л�ͼ��');
    grid on;
    
    figure;
    plot(dataSpeed);
    xlabel('�ٶ�');
    ylabel('�ٶȵ�λ');
    title('�ٶ��л�ͼ��');
    grid on;
    
    figure;
    plot(dataDirection);
    xlabel('����');
    ylabel('�����л���λ');
    title('�����л�ͼ��');
    grid on;
    
    result = true;
end